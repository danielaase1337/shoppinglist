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
    /// Unit tests for MealRecipeController.
    /// All tests call actual controller methods (RunAll / RunOne) — not the mock directly.
    ///
    /// Tests 7-8 (SoftDelete): Document EXPECTED soft-delete behaviour.
    ///   Current controller calls _repository.Delete(id) (hard delete — bool return).
    ///   These tests will FAIL until the controller is updated to:
    ///     Get → set IsActive=false → Update   (soft-delete pattern)
    ///
    /// Test 9 (BulkImport): Marked Skip — RunBulkImport endpoint does not exist yet.
    ///   Remove Skip and uncomment the controller call once Glenn adds the endpoint.
    /// </summary>
    public class MealRecipeControllerTests
    {
        private readonly Mock<IGenericRepository<MealRecipe>> _mockRepository;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMapper _mapper;
        private readonly MealRecipeController _controller;

        private static readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public MealRecipeControllerTests()
        {
            _mockRepository = new Mock<IGenericRepository<MealRecipe>>();
            _loggerFactory = NullLoggerFactory.Instance;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<MealRecipe, MealRecipeModel>().ReverseMap();
                cfg.CreateMap<MealIngredient, MealIngredientModel>().ReverseMap();
                cfg.CreateMap<ShopItem, ShopItemModel>().ReverseMap();
                cfg.CreateMap<ItemCategory, ItemCategoryModel>().ReverseMap();
            });
            _mapper = config.CreateMapper();

            _controller = new MealRecipeController(_loggerFactory, _mockRepository.Object, _mapper);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static List<MealRecipe> CreateSampleRecipes() => new List<MealRecipe>
        {
            new MealRecipe
            {
                Id = "recipe-1",
                Name = "Taco",
                Category = FireStoreMealCategory.KidsLike,
                PopularityScore = 47,
                IsActive = true,
                LastModified = DateTime.UtcNow.AddDays(-5),
                Ingredients = new List<MealIngredient>()
            },
            new MealRecipe
            {
                Id = "recipe-2",
                Name = "Fiskegrateng",
                Category = FireStoreMealCategory.Fish,
                PopularityScore = 28,
                IsActive = true,
                LastModified = DateTime.UtcNow.AddDays(-2),
                Ingredients = new List<MealIngredient>()
            },
            new MealRecipe
            {
                Id = "recipe-3",
                Name = "Pizza",
                Category = FireStoreMealCategory.KidsLike,
                PopularityScore = 42,
                IsActive = true,
                LastModified = DateTime.UtcNow,
                Ingredients = new List<MealIngredient>()
            }
        };

        private static async Task<T?> ReadBody<T>(HttpResponseData response)
        {
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            return JsonSerializer.Deserialize<T>(body, _jsonOptions);
        }

        // ── Test 1: GetAll returns all recipes ───────────────────────────────────

        [Fact]
        public async Task GetAll_ReturnsAllRecipes()
        {
            // Arrange
            var recipes = CreateSampleRecipes();
            _mockRepository.Setup(r => r.Get()).ReturnsAsync(recipes);
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/mealrecipes");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<MealRecipeModel[]>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(3, result.Length);
            Assert.Contains(result, r => r.Name == "Taco");
            Assert.Contains(result, r => r.Name == "Fiskegrateng");
            Assert.Contains(result, r => r.Name == "Pizza");
        }

        // ── Test 2: GetAll returns empty list when no recipes ────────────────────

        [Fact]
        public async Task GetAll_ReturnsEmptyList_WhenNoRecipes()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get()).ReturnsAsync(new List<MealRecipe>());
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/mealrecipes");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<MealRecipeModel[]>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // ── Test 3: GetById returns correct recipe ───────────────────────────────

        [Fact]
        public async Task GetById_ReturnsRecipe_WhenExists()
        {
            // Arrange
            var recipe = new MealRecipe
            {
                Id = "recipe-1",
                Name = "Taco",
                Category = FireStoreMealCategory.KidsLike,
                PopularityScore = 47,
                IsActive = true,
                Ingredients = new List<MealIngredient>()
            };
            _mockRepository.Setup(r => r.Get("recipe-1")).ReturnsAsync(recipe);
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/mealrecipe/recipe-1");

            // Act
            var response = await _controller.RunOne(req, "recipe-1");
            var result = await ReadBody<MealRecipeModel>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal("Taco", result.Name);
            Assert.Equal("recipe-1", result.Id);
            Assert.True(result.IsActive);
            Assert.Equal(47, result.PopularityScore);
        }

        // ── Test 4: GetById returns 404 for unknown id ───────────────────────────

        [Fact]
        public async Task GetById_Returns404_WhenNotFound()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get("nonexistent")).ReturnsAsync((MealRecipe?)null);
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/mealrecipe/nonexistent");

            // Act
            var response = await _controller.RunOne(req, "nonexistent");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ── Test 5: POST creates recipe and sets LastModified ────────────────────

        [Fact]
        public async Task Create_CreatesRecipe_AndSetsLastModified()
        {
            // Arrange
            var newRecipeModel = new MealRecipeModel
            {
                Id = "recipe-new",
                Name = "Biff Stroganoff",
                Category = DtoMealCategory.Meat,
                PopularityScore = 0,
                IsActive = true
            };

            var savedRecipe = _mapper.Map<MealRecipe>(newRecipeModel);
            savedRecipe.LastModified = DateTime.UtcNow;

            _mockRepository
                .Setup(r => r.Insert(It.IsAny<MealRecipe>()))
                .ReturnsAsync(savedRecipe);

            var body = JsonSerializer.Serialize(newRecipeModel);
            var req = TestHttpFactory.CreatePostRequest(body, "http://localhost/api/mealrecipes");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<MealRecipeModel>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal("Biff Stroganoff", result.Name);
            Assert.NotNull(result.LastModified);

            // Verify the controller set LastModified before calling Insert
            _mockRepository.Verify(r => r.Insert(It.Is<MealRecipe>(
                mr => mr.LastModified.HasValue
            )), Times.Once);
        }

        // ── Test 6: PUT updates recipe and refreshes LastModified ────────────────

        [Fact]
        public async Task Update_UpdatesRecipe_AndUpdatesLastModified()
        {
            // Arrange
            var oldTimestamp = DateTime.UtcNow.AddDays(-5);
            var existingModel = new MealRecipeModel
            {
                Id = "recipe-1",
                Name = "Taco - Oppdatert",
                Category = DtoMealCategory.KidsLike,
                PopularityScore = 50,
                IsActive = true,
                LastModified = oldTimestamp
            };

            var updatedRecipe = _mapper.Map<MealRecipe>(existingModel);
            updatedRecipe.LastModified = DateTime.UtcNow; // controller refreshes this

            _mockRepository
                .Setup(r => r.Update(It.IsAny<MealRecipe>()))
                .ReturnsAsync(updatedRecipe);

            var body = JsonSerializer.Serialize(existingModel);
            var req = TestHttpFactory.CreatePutRequest(body, "http://localhost/api/mealrecipes");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<MealRecipeModel>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal("Taco - Oppdatert", result.Name);
            Assert.Equal(50, result.PopularityScore);
            Assert.NotNull(result.LastModified);

            // Verify controller refreshed LastModified (newer than old value) before Update
            _mockRepository.Verify(r => r.Update(It.Is<MealRecipe>(
                mr => mr.LastModified.HasValue && mr.LastModified.Value > oldTimestamp
            )), Times.Once);
        }

        // ── Test 7: DELETE performs soft delete (sets IsActive=false) ────────────
        //
        // EXPECTED: Get recipe → set IsActive=false → call Update (not Delete).
        // CURRENT:  calls _repository.Delete(id) which returns bool, then tries to
        //           map that bool as MealRecipeModel — this is a bug in the controller.
        // STATUS:   This test FAILS until MealRecipeController.RunOne DELETE is fixed
        //           to implement the soft-delete pattern.

        [Fact]
        public async Task SoftDelete_SetsIsActiveToFalse()
        {
            // Arrange
            var existingRecipe = new MealRecipe
            {
                Id = "recipe-1",
                Name = "Taco",
                Category = FireStoreMealCategory.KidsLike,
                PopularityScore = 47,
                IsActive = true,
                Ingredients = new List<MealIngredient>()
            };

            var softDeletedRecipe = new MealRecipe
            {
                Id = "recipe-1",
                Name = "Taco",
                Category = FireStoreMealCategory.KidsLike,
                PopularityScore = 47,
                IsActive = false,
                LastModified = DateTime.UtcNow,
                Ingredients = new List<MealIngredient>()
            };

            _mockRepository.Setup(r => r.Get("recipe-1")).ReturnsAsync(existingRecipe);
            _mockRepository
                .Setup(r => r.Update(It.Is<MealRecipe>(mr => !mr.IsActive)))
                .ReturnsAsync(softDeletedRecipe);

            var req = TestHttpFactory.CreateDeleteRequest("http://localhost/api/mealrecipe/recipe-1");

            // Act
            var response = await _controller.RunOne(req, "recipe-1");
            var result = await ReadBody<MealRecipeModel>(response);

            // Assert: response contains the recipe with IsActive=false
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.False(result.IsActive);

            // Verify soft-delete pattern: Update was called (not hard Delete)
            _mockRepository.Verify(r => r.Update(It.Is<MealRecipe>(mr => !mr.IsActive)), Times.Once);
            _mockRepository.Verify(r => r.Delete(It.IsAny<object>()), Times.Never);
        }

        // ── Test 8: DELETE returns 404 for unknown id ────────────────────────────
        //
        // EXPECTED: Get returns null → 404. Depends on soft-delete (Get first) pattern.

        [Fact]
        public async Task SoftDelete_Returns404_WhenNotFound()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get("nonexistent")).ReturnsAsync((MealRecipe?)null);
            var req = TestHttpFactory.CreateDeleteRequest("http://localhost/api/mealrecipe/nonexistent");

            // Act
            var response = await _controller.RunOne(req, "nonexistent");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ── Test 9: POST /import bulk-creates all recipes ────────────────────────

        [Fact]
        public async Task BulkImport_CreatesAllRecipes()
        {
            // Arrange
            var recipesToImport = new List<MealRecipeModel>
            {
                new MealRecipeModel
                {
                    Name = "Kjøttboller",
                    Category = DtoMealCategory.KidsLike,
                    PopularityScore = 0,
                    IsActive = true
                },
                new MealRecipeModel
                {
                    Name = "Laks med grønnsaker",
                    Category = DtoMealCategory.Fish,
                    PopularityScore = 0,
                    IsActive = true
                }
            };

            var saved0 = _mapper.Map<MealRecipe>(recipesToImport[0]);
            saved0.Id = "bulk-0";
            saved0.LastModified = DateTime.UtcNow;

            var saved1 = _mapper.Map<MealRecipe>(recipesToImport[1]);
            saved1.Id = "bulk-1";
            saved1.LastModified = DateTime.UtcNow;

            _mockRepository.SetupSequence(r => r.Insert(It.IsAny<MealRecipe>()))
                .ReturnsAsync(saved0)
                .ReturnsAsync(saved1);

            var body = JsonSerializer.Serialize(recipesToImport);
            var req = TestHttpFactory.CreatePostRequest(body, "http://localhost/api/mealrecipes/import");

            // Act
            var response = await _controller.RunImport(req);
            var result = await ReadBody<MealRecipeModel[]>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(2, result.Length);
            Assert.All(result, r => Assert.NotNull(r.LastModified));
            Assert.All(result, r => Assert.True(r.IsActive));
            _mockRepository.Verify(r => r.Insert(It.IsAny<MealRecipe>()), Times.Exactly(2));
        }

        // ── Test 10: GET returns recipes sorted by PopularityScore DESC ──────────

        [Fact]
        public async Task GetAll_SortedByPopularityDescending()
        {
            // Arrange: scores are 47, 28, 42 — expected order after sort: 47, 42, 28
            var recipes = CreateSampleRecipes();
            _mockRepository.Setup(r => r.Get()).ReturnsAsync(recipes);
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/mealrecipes");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<MealRecipeModel[]>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(3, result.Length);
            Assert.Equal("Taco", result[0].Name);           // score 47
            Assert.Equal("Pizza", result[1].Name);          // score 42
            Assert.Equal("Fiskegrateng", result[2].Name);   // score 28
            Assert.True(result[0].PopularityScore >= result[1].PopularityScore);
            Assert.True(result[1].PopularityScore >= result[2].PopularityScore);
        }

        // ── Model validation (kept from original — these are still useful) ───────

        [Fact]
        public void MealRecipeModel_IsValid_ReturnsFalse_WhenNameIsEmpty()
        {
            var model = new MealRecipeModel
            {
                Name = "",
                Category = DtoMealCategory.Meat,
                IsActive = true,
                Ingredients = new List<MealIngredientModel>()
            };

            Assert.False(model.IsValid());
        }

        [Fact]
        public void MealRecipeModel_IsValid_ReturnsTrue_WhenNameIsSet()
        {
            var model = new MealRecipeModel
            {
                Name = "Taco",
                Category = DtoMealCategory.KidsLike,
                IsActive = true,
                Ingredients = new List<MealIngredientModel>()
            };

            Assert.True(model.IsValid());
        }
    }
}
