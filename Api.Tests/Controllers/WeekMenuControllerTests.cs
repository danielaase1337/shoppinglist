using Api.Controllers;
using Api.Tests.Helpers;
using AutoMapper;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.FireStoreDataModels;
using Shared.HandlelisteModels;
using Shared.Repository;
using System.Net;
using System.Text.Json;
using Xunit;

// MealCategory exists in both Shared.FireStoreDataModels and Shared.HandlelisteModels.
// Use explicit aliases to avoid ambiguity.
using FireStoreMealCategory = Shared.FireStoreDataModels.MealCategory;
using DtoMealCategory = Shared.HandlelisteModels.MealCategory;

namespace Api.Tests.Controllers
{
    /// <summary>
    /// Unit tests for WeekMenuController.
    /// All tests call actual controller methods (RunAll / RunOne / RunByWeek / RunGenerateShoppingList)
    /// — not the mocks directly.
    ///
    /// ⚠️  WeekMenuController does not exist yet when these tests were written.
    ///     Tests are based on the spec and will compile once Glenn creates
    ///     Api/Controllers/WeekMenuController.cs.
    ///
    /// Expected constructor signature:
    ///   WeekMenuController(ILoggerFactory, IGenericRepository{WeekMenu},
    ///                      IGenericRepository{MealRecipe}, IMapper)
    ///
    /// Expected method signatures:
    ///   RunAll(HttpRequestData req)                              — GET/POST/PUT /api/weekmenus
    ///   RunOne(HttpRequestData req, string id)                  — GET/DELETE /api/weekmenu/{id}
    ///   RunByWeek(HttpRequestData req, int weekNumber, int year) — GET /api/weekmenu/week/{n}/year/{y}
    ///   RunGenerateShoppingList(HttpRequestData req, string id)  — POST /api/weekmenu/{id}/generate-shoppinglist
    /// </summary>
    public class WeekMenuControllerTests
    {
        private readonly Mock<IGenericRepository<WeekMenu>> _mockWeekMenuRepository;
        private readonly Mock<IGenericRepository<MealRecipe>> _mockMealRecipeRepository;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMapper _mapper;
        private readonly WeekMenuController _controller;

        private static readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public WeekMenuControllerTests()
        {
            _mockWeekMenuRepository = new Mock<IGenericRepository<WeekMenu>>();
            _mockMealRecipeRepository = new Mock<IGenericRepository<MealRecipe>>();
            _loggerFactory = NullLoggerFactory.Instance;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<WeekMenu, WeekMenuModel>().ReverseMap();
                cfg.CreateMap<DailyMeal, DailyMealModel>().ReverseMap();
                cfg.CreateMap<MealRecipe, MealRecipeModel>().ReverseMap();
                cfg.CreateMap<MealIngredient, MealIngredientModel>().ReverseMap();
                cfg.CreateMap<ShopItem, ShopItemModel>().ReverseMap();
                cfg.CreateMap<ItemCategory, ItemCategoryModel>().ReverseMap();
            });
            _mapper = config.CreateMapper();

            _controller = new WeekMenuController(
                _loggerFactory,
                _mockWeekMenuRepository.Object,
                _mockMealRecipeRepository.Object,
                _mapper);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static async Task<T?> ReadBody<T>(HttpResponseData response)
        {
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            return JsonSerializer.Deserialize<T>(body, _jsonOptions);
        }

        private static List<WeekMenu> CreateSampleMenus() => new List<WeekMenu>
        {
            new WeekMenu
            {
                Id = "menu-1",
                Name = "Uke 47 2025",
                WeekNumber = 47,
                Year = 2025,
                IsActive = true,
                LastModified = DateTime.UtcNow.AddDays(-7),
                DailyMeals = new List<DailyMeal>()
            },
            new WeekMenu
            {
                Id = "menu-2",
                Name = "Uke 48 2025",
                WeekNumber = 48,
                Year = 2025,
                IsActive = true,
                LastModified = DateTime.UtcNow,
                DailyMeals = new List<DailyMeal>()
            }
        };

        // ── Test 1: GetAll returns all active menus ───────────────────────────────

        [Fact]
        public async Task GetAll_ReturnsAllActiveMenus()
        {
            // Arrange: two menus — week 48 (newer) should appear before week 47
            var menus = CreateSampleMenus();
            _mockWeekMenuRepository.Setup(r => r.Get()).ReturnsAsync(menus);
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/weekmenus");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<WeekMenuModel[]>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(2, result.Length);
            Assert.Contains(result, m => m.Name == "Uke 47 2025");
            Assert.Contains(result, m => m.Name == "Uke 48 2025");

            // Verify ordering: Year DESC, WeekNumber DESC — week 48 before week 47
            Assert.Equal("Uke 48 2025", result[0].Name);
            Assert.Equal("Uke 47 2025", result[1].Name);
        }

        // ── Test 2: GetAll returns empty list when no menus ───────────────────────

        [Fact]
        public async Task GetAll_ReturnsEmptyList_WhenNoMenus()
        {
            // Arrange
            _mockWeekMenuRepository.Setup(r => r.Get()).ReturnsAsync(new List<WeekMenu>());
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/weekmenus");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<WeekMenuModel[]>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // ── Test 3: GetAll returns error when repository returns null ─────────────

        [Fact]
        public async Task GetAll_ReturnsError_WhenRepositoryReturnsNull()
        {
            // Arrange
            _mockWeekMenuRepository.Setup(r => r.Get()).ReturnsAsync((List<WeekMenu>?)null);
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/weekmenus");

            // Act
            var response = await _controller.RunAll(req);

            // Assert: ControllerBase.GetErroRespons returns 500
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ── Test 4: POST sets IsActive=true and LastModified ─────────────────────

        [Fact]
        public async Task Create_SetsIsActiveAndLastModified()
        {
            // Arrange
            var newMenuModel = new WeekMenuModel
            {
                Id = "menu-new",
                Name = "Uke 49 2025",
                WeekNumber = 49,
                Year = 2025,
                IsActive = false // controller should override this to true
            };

            var savedMenu = _mapper.Map<WeekMenu>(newMenuModel);
            savedMenu.IsActive = true;
            savedMenu.LastModified = DateTime.UtcNow;

            _mockWeekMenuRepository
                .Setup(r => r.Insert(It.IsAny<WeekMenu>()))
                .ReturnsAsync(savedMenu);

            var body = JsonSerializer.Serialize(newMenuModel);
            var req = TestHttpFactory.CreatePostRequest(body, "http://localhost/api/weekmenus");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<WeekMenuModel>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.True(result.IsActive);
            Assert.NotNull(result.LastModified);

            // Verify controller set both IsActive and LastModified before calling Insert
            _mockWeekMenuRepository.Verify(r => r.Insert(It.Is<WeekMenu>(
                wm => wm.IsActive && wm.LastModified.HasValue
            )), Times.Once);
        }

        // ── Test 5: POST auto-sets Name when empty ────────────────────────────────

        [Fact]
        public async Task Create_SetsNameAutomatically_WhenNameEmpty()
        {
            // Arrange: Name is empty — controller should set "Uke {WeekNumber} {Year}"
            var newMenuModel = new WeekMenuModel
            {
                Name = "",
                WeekNumber = 50,
                Year = 2025,
                IsActive = true
            };

            WeekMenu? capturedMenu = null;
            _mockWeekMenuRepository
                .Setup(r => r.Insert(It.IsAny<WeekMenu>()))
                .Callback<WeekMenu>(wm => capturedMenu = wm)
                .ReturnsAsync((WeekMenu wm) =>
                {
                    wm.Id = "menu-auto";
                    return wm;
                });

            var body = JsonSerializer.Serialize(newMenuModel);
            var req = TestHttpFactory.CreatePostRequest(body, "http://localhost/api/weekmenus");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<WeekMenuModel>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal("Uke 50 2025", result.Name);

            // Verify the name was set before Insert
            Assert.NotNull(capturedMenu);
            Assert.Equal("Uke 50 2025", capturedMenu!.Name);
        }

        // ── Test 6: PUT updates LastModified ──────────────────────────────────────

        [Fact]
        public async Task Update_SetsLastModified()
        {
            // Arrange
            var oldTimestamp = DateTime.UtcNow.AddDays(-5);
            var existingModel = new WeekMenuModel
            {
                Id = "menu-1",
                Name = "Uke 47 2025",
                WeekNumber = 47,
                Year = 2025,
                IsActive = true,
                LastModified = oldTimestamp
            };

            var updatedMenu = _mapper.Map<WeekMenu>(existingModel);
            updatedMenu.LastModified = DateTime.UtcNow; // controller refreshes this

            _mockWeekMenuRepository
                .Setup(r => r.Update(It.IsAny<WeekMenu>()))
                .ReturnsAsync(updatedMenu);

            var body = JsonSerializer.Serialize(existingModel);
            var req = TestHttpFactory.CreatePutRequest(body, "http://localhost/api/weekmenus");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<WeekMenuModel>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotNull(result.LastModified);

            // Verify controller set a newer LastModified before calling Update
            _mockWeekMenuRepository.Verify(r => r.Update(It.Is<WeekMenu>(
                wm => wm.LastModified.HasValue && wm.LastModified.Value > oldTimestamp
            )), Times.Once);
        }

        // ── Test 7: GET /{id} returns correct menu ────────────────────────────────

        [Fact]
        public async Task GetById_ReturnsMenu_WhenFound()
        {
            // Arrange
            var menu = new WeekMenu
            {
                Id = "menu-1",
                Name = "Uke 47 2025",
                WeekNumber = 47,
                Year = 2025,
                IsActive = true,
                DailyMeals = new List<DailyMeal>()
            };
            _mockWeekMenuRepository.Setup(r => r.Get("menu-1")).ReturnsAsync(menu);
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/weekmenu/menu-1");

            // Act
            var response = await _controller.RunOne(req, "menu-1");
            var result = await ReadBody<WeekMenuModel>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal("menu-1", result.Id);
            Assert.Equal("Uke 47 2025", result.Name);
            Assert.Equal(47, result.WeekNumber);
            Assert.Equal(2025, result.Year);
            Assert.True(result.IsActive);
        }

        // ── Test 8: GET /{id} returns 404 for unknown id ──────────────────────────

        [Fact]
        public async Task GetById_Returns404_WhenNotFound()
        {
            // Arrange
            _mockWeekMenuRepository.Setup(r => r.Get("nonexistent")).ReturnsAsync((WeekMenu?)null);
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/weekmenu/nonexistent");

            // Act
            var response = await _controller.RunOne(req, "nonexistent");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ── Test 9: DELETE performs soft delete — never calls _repository.Delete ──

        [Fact]
        public async Task Delete_SoftDeletes_DoesNotCallRepositoryDelete()
        {
            // Arrange
            var existingMenu = new WeekMenu
            {
                Id = "menu-1",
                Name = "Uke 47 2025",
                WeekNumber = 47,
                Year = 2025,
                IsActive = true,
                DailyMeals = new List<DailyMeal>()
            };

            var softDeletedMenu = new WeekMenu
            {
                Id = "menu-1",
                Name = "Uke 47 2025",
                WeekNumber = 47,
                Year = 2025,
                IsActive = false,
                LastModified = DateTime.UtcNow,
                DailyMeals = new List<DailyMeal>()
            };

            _mockWeekMenuRepository.Setup(r => r.Get("menu-1")).ReturnsAsync(existingMenu);
            _mockWeekMenuRepository
                .Setup(r => r.Update(It.Is<WeekMenu>(wm => !wm.IsActive)))
                .ReturnsAsync(softDeletedMenu);

            var req = TestHttpFactory.CreateDeleteRequest("http://localhost/api/weekmenu/menu-1");

            // Act
            var response = await _controller.RunOne(req, "menu-1");
            var result = await ReadBody<WeekMenuModel>(response);

            // Assert: response returns the soft-deleted menu
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.False(result.IsActive);

            // Verify soft-delete pattern: Update was called (not hard Delete)
            _mockWeekMenuRepository.Verify(r => r.Update(It.Is<WeekMenu>(
                wm => !wm.IsActive && wm.LastModified.HasValue
            )), Times.Once);
            _mockWeekMenuRepository.Verify(r => r.Delete(It.IsAny<object>()), Times.Never);
        }

        // ── Test 10: DELETE returns 404 for unknown id ────────────────────────────

        [Fact]
        public async Task Delete_Returns404_WhenNotFound()
        {
            // Arrange
            _mockWeekMenuRepository.Setup(r => r.Get("nonexistent")).ReturnsAsync((WeekMenu?)null);
            var req = TestHttpFactory.CreateDeleteRequest("http://localhost/api/weekmenu/nonexistent");

            // Act
            var response = await _controller.RunOne(req, "nonexistent");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ── Test 11: GET /weekmenu/week/{n}/year/{y} returns menu when found ──────

        [Fact]
        public async Task GetByWeek_ReturnsMenu_WhenFound()
        {
            // Arrange: three menus — only week 47, 2025, IsActive=true should match
            var menus = new List<WeekMenu>
            {
                new WeekMenu { Id = "menu-1", Name = "Uke 46 2025", WeekNumber = 46, Year = 2025, IsActive = true, DailyMeals = new List<DailyMeal>() },
                new WeekMenu { Id = "menu-2", Name = "Uke 47 2025", WeekNumber = 47, Year = 2025, IsActive = true, DailyMeals = new List<DailyMeal>() },
                new WeekMenu { Id = "menu-3", Name = "Uke 47 2024", WeekNumber = 47, Year = 2024, IsActive = true, DailyMeals = new List<DailyMeal>() }
            };
            _mockWeekMenuRepository.Setup(r => r.Get()).ReturnsAsync(menus);
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/weekmenu/week/47/year/2025");

            // Act
            var response = await _controller.RunByWeek(req, 47, 2025);
            var result = await ReadBody<WeekMenuModel>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal("menu-2", result.Id);
            Assert.Equal(47, result.WeekNumber);
            Assert.Equal(2025, result.Year);
        }

        // ── Test 12: GET /weekmenu/week/{n}/year/{y} returns 404 when not found ───

        [Fact]
        public async Task GetByWeek_Returns404_WhenNotFound()
        {
            // Arrange: no menu for week 99, 2025
            _mockWeekMenuRepository.Setup(r => r.Get()).ReturnsAsync(new List<WeekMenu>
            {
                new WeekMenu { Id = "menu-1", WeekNumber = 47, Year = 2025, IsActive = true, DailyMeals = new List<DailyMeal>() }
            });
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/weekmenu/week/99/year/2025");

            // Act
            var response = await _controller.RunByWeek(req, 99, 2025);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ── Test 13: Generate shopping list aggregates same ShopItemId ────────────

        [Fact]
        public async Task GenerateShoppingList_AggregatesIngredients()
        {
            // Arrange: two meals each using the same ShopItemId with different quantities
            const string sharedItemId = "item-pasta";
            const string sharedItemName = "Pasta";

            var recipe1 = new MealRecipe
            {
                Id = "recipe-1",
                Name = "Bolognese",
                IsActive = true,
                Ingredients = new List<MealIngredient>
                {
                    new MealIngredient { ShopItemId = sharedItemId, ShopItemName = sharedItemName, Quantity = 400 }
                }
            };

            var recipe2 = new MealRecipe
            {
                Id = "recipe-2",
                Name = "Pasta med kylling",
                IsActive = true,
                Ingredients = new List<MealIngredient>
                {
                    new MealIngredient { ShopItemId = sharedItemId, ShopItemName = sharedItemName, Quantity = 300 }
                }
            };

            var menu = new WeekMenu
            {
                Id = "menu-agg",
                Name = "Uke 47 2025",
                WeekNumber = 47,
                Year = 2025,
                IsActive = true,
                DailyMeals = new List<DailyMeal>
                {
                    new DailyMeal { Day = DayOfWeek.Monday, MealRecipeId = "recipe-1", CustomIngredients = new List<MealIngredient>() },
                    new DailyMeal { Day = DayOfWeek.Tuesday, MealRecipeId = "recipe-2", CustomIngredients = new List<MealIngredient>() }
                }
            };

            _mockWeekMenuRepository.Setup(r => r.Get("menu-agg")).ReturnsAsync(menu);
            // Use Returns(Task.FromResult) with explicit ICollection<T> to avoid type inference
            // issues between List<T> and ICollection<T> in Moq's ReturnsAsync overload resolution.
            _mockMealRecipeRepository
                .Setup(r => r.Get())
                .Returns(Task.FromResult<ICollection<MealRecipe>>(new List<MealRecipe> { recipe1, recipe2 }));

            var req = TestHttpFactory.CreatePostRequest("", "http://localhost/api/weekmenu/menu-agg/generate-shoppinglist");

            // Act
            var response = await _controller.RunGenerateShoppingList(req, "menu-agg");
            var result = await ReadBody<ShoppingListModel>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal("Ukemeny Uke 47 2025", result.Name);

            // Same ShopItemId from two meals must be aggregated into one item
            Assert.Single(result.ShoppingItems);
            var item = result.ShoppingItems.Single();
            Assert.Equal(sharedItemId, item.Varen.Id);
            Assert.Equal(700, item.Mengde); // 400 + 300
        }

        // ── Test 14: Generate uses CustomIngredients when present ─────────────────

        [Fact]
        public async Task GenerateShoppingList_UsesCustomIngredients_WhenPresent()
        {
            // Arrange: DailyMeal has CustomIngredients — these should override recipe ingredients
            const string recipeItemId = "item-from-recipe";
            const string customItemId = "item-from-custom";

            var recipe = new MealRecipe
            {
                Id = "recipe-1",
                Name = "Taco",
                IsActive = true,
                Ingredients = new List<MealIngredient>
                {
                    new MealIngredient { ShopItemId = recipeItemId, ShopItemName = "Recipe Item", Quantity = 500 }
                }
            };

            var menu = new WeekMenu
            {
                Id = "menu-custom",
                Name = "Uke 47 2025",
                WeekNumber = 47,
                Year = 2025,
                IsActive = true,
                DailyMeals = new List<DailyMeal>
                {
                    new DailyMeal
                    {
                        Day = DayOfWeek.Monday,
                        MealRecipeId = "recipe-1",
                        // CustomIngredients override the recipe's ingredients
                        CustomIngredients = new List<MealIngredient>
                        {
                            new MealIngredient { ShopItemId = customItemId, ShopItemName = "Custom Item", Quantity = 200 }
                        }
                    }
                }
            };

            _mockWeekMenuRepository.Setup(r => r.Get("menu-custom")).ReturnsAsync(menu);
            // Controller always calls _mealRepository.Get() to build a lookup dict,
            // even when custom ingredients are present — it just won't use the recipe's ingredients.
            _mockMealRecipeRepository
                .Setup(r => r.Get())
                .Returns(Task.FromResult<ICollection<MealRecipe>>(new List<MealRecipe> { recipe }));

            var req = TestHttpFactory.CreatePostRequest("", "http://localhost/api/weekmenu/menu-custom/generate-shoppinglist");

            // Act
            var response = await _controller.RunGenerateShoppingList(req, "menu-custom");
            var result = await ReadBody<ShoppingListModel>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Single(result.ShoppingItems);

            var item = result.ShoppingItems.Single();
            Assert.Equal(customItemId, item.Varen.Id);     // custom item used
            Assert.NotEqual(recipeItemId, item.Varen.Id);  // recipe item NOT used
        }

        // ── Test 15: Generate returns 404 when WeekMenu not found ─────────────────

        [Fact]
        public async Task GenerateShoppingList_Returns404_WhenMenuNotFound()
        {
            // Arrange
            _mockWeekMenuRepository.Setup(r => r.Get("nonexistent")).ReturnsAsync((WeekMenu?)null);
            var req = TestHttpFactory.CreatePostRequest("", "http://localhost/api/weekmenu/nonexistent/generate-shoppinglist");

            // Act
            var response = await _controller.RunGenerateShoppingList(req, "nonexistent");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
