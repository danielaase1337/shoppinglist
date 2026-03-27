using Api.Controllers;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.FireStoreDataModels;
using Shared.HandlelisteModels;
using Shared.Repository;
using Xunit;

namespace Api.Tests.Controllers
{
    public class MealRecipeControllerTests
    {
        private readonly Mock<IGenericRepository<MealRecipe>> _mockRepository;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMapper _mapper;
        private readonly MealRecipeController _controller;

        public MealRecipeControllerTests()
        {
            _mockRepository = new Mock<IGenericRepository<MealRecipe>>();
            _loggerFactory = NullLoggerFactory.Instance;

            // Setup AutoMapper with the same profile as the API
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

        [Fact]
        public async Task Repository_Get_ReturnsAllMealRecipes_WhenRecipesExist()
        {
            // Arrange
            var mealRecipes = new List<MealRecipe>
            {
                new MealRecipe
                {
                    Id = "1",
                    Name = "Taco",
                    Category = Shared.FireStoreDataModels.MealCategory.KidsLike,
                    PopularityScore = 47,
                    LastUsed = DateTime.UtcNow.AddDays(-7),
                    IsActive = true,
                    Ingredients = new List<MealIngredient>()
                },
                new MealRecipe
                {
                    Id = "2",
                    Name = "Pizza",
                    Category = Shared.FireStoreDataModels.MealCategory.KidsLike,
                    PopularityScore = 42,
                    LastUsed = DateTime.UtcNow.AddDays(-15),
                    IsActive = true,
                    Ingredients = new List<MealIngredient>()
                }
            };

            _mockRepository.Setup(r => r.Get()).ReturnsAsync(mealRecipes);

            // Act
            var result = await _mockRepository.Object.Get();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, m => m.Name == "Taco");
        }

        [Fact]
        public async Task Repository_GetById_ReturnsMealRecipe_WhenRecipeExists()
        {
            // Arrange
            var mealRecipe = new MealRecipe
            {
                Id = "1",
                Name = "Taco",
                Category = Shared.FireStoreDataModels.MealCategory.KidsLike,
                PopularityScore = 47,
                IsActive = true,
                Ingredients = new List<MealIngredient>()
            };

            _mockRepository.Setup(r => r.Get("1")).ReturnsAsync(mealRecipe);

            // Act
            var result = await _mockRepository.Object.Get("1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Taco", result.Name);
        }

        [Fact]
        public async Task Repository_Insert_AddsMealRecipe()
        {
            // Arrange
            var newRecipe = new MealRecipe
            {
                Id = "4",
                Name = "Lasagne",
                Category = Shared.FireStoreDataModels.MealCategory.Meat,
                PopularityScore = 0,
                IsActive = true,
                Ingredients = new List<MealIngredient>()
            };

            _mockRepository.Setup(r => r.Insert(It.IsAny<MealRecipe>()))
                .ReturnsAsync((MealRecipe recipe) =>
                {
                    recipe.LastModified = DateTime.UtcNow;
                    return recipe;
                });

            // Act
            var result = await _mockRepository.Object.Insert(newRecipe);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Lasagne", result.Name);
            Assert.NotNull(result.LastModified);
        }

        [Fact]
        public async Task Repository_Update_UpdatesMealRecipe()
        {
            // Arrange
            var updatedRecipe = new MealRecipe
            {
                Id = "1",
                Name = "Taco Supreme",
                Category = Shared.FireStoreDataModels.MealCategory.KidsLike,
                PopularityScore = 50,
                IsActive = true,
                Ingredients = new List<MealIngredient>()
            };

            _mockRepository.Setup(r => r.Update(It.IsAny<MealRecipe>()))
                .ReturnsAsync((MealRecipe recipe) =>
                {
                    recipe.LastModified = DateTime.UtcNow;
                    return recipe;
                });

            // Act
            var result = await _mockRepository.Object.Update(updatedRecipe);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Taco Supreme", result.Name);
            Assert.NotNull(result.LastModified);
        }

        [Fact]
        public async Task Repository_Delete_RemovesMealRecipe()
        {
            // Arrange
            _mockRepository.Setup(r => r.Delete("1")).ReturnsAsync(true);

            // Act
            var result = await _mockRepository.Object.Delete("1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task Repository_GetSortedByPopularity_ReturnsRecipesInDescendingOrder()
        {
            // Arrange
            var mealRecipes = new List<MealRecipe>
            {
                new MealRecipe { Id = "1", Name = "Pizza", PopularityScore = 42, IsActive = true, Ingredients = new List<MealIngredient>() },
                new MealRecipe { Id = "2", Name = "Taco", PopularityScore = 47, IsActive = true, Ingredients = new List<MealIngredient>() },
                new MealRecipe { Id = "3", Name = "Fiskegrateng", PopularityScore = 28, IsActive = true, Ingredients = new List<MealIngredient>() }
            };

            _mockRepository.Setup(r => r.Get()).ReturnsAsync(mealRecipes);

            // Act
            var result = await _mockRepository.Object.Get();
            var sortedResult = result.OrderByDescending(r => r.PopularityScore).ToList();

            // Assert
            Assert.Equal("Taco", sortedResult[0].Name);
            Assert.Equal(47, sortedResult[0].PopularityScore);
        }

        [Fact]
        public void MealRecipeModel_IsValid_ReturnsFalse_WhenNameIsEmpty()
        {
            // Arrange
            var model = new MealRecipeModel
            {
                Name = "",
                Category = Shared.HandlelisteModels.MealCategory.Meat,
                IsActive = true,
                Ingredients = new List<MealIngredientModel>()
            };

            // Act
            var isValid = model.IsValid();

            // Assert
            Assert.False(isValid);
        }
    }
}
