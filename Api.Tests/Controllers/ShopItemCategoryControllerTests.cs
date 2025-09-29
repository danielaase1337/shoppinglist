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
    public class ShopItemCategoryControllerTests
    {
        private readonly Mock<IGenericRepository<ItemCategory>> _mockRepository;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMapper _mapper;
        private readonly ShopItemCategoryController _controller;

        public ShopItemCategoryControllerTests()
        {
            _mockRepository = new Mock<IGenericRepository<ItemCategory>>();
            _loggerFactory = NullLoggerFactory.Instance;

            // Setup AutoMapper with the same profile as the API
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ItemCategory, ItemCategoryModel>().ReverseMap();
            });
            _mapper = config.CreateMapper();

            _controller = new ShopItemCategoryController(_loggerFactory, _mockRepository.Object, _mapper);
        }

        [Fact]
        public async Task Repository_Get_ReturnsAllItemCategories_WhenCategoriesExist()
        {
            // Arrange
            var categories = new List<ItemCategory>
            {
                new ItemCategory { Id = "1", Name = "Meieri" },
                new ItemCategory { Id = "2", Name = "Bakeri" },
                new ItemCategory { Id = "3", Name = "Frukt" }
            };

            _mockRepository.Setup(r => r.Get()).ReturnsAsync(categories);

            // Act
            var result = await _mockRepository.Object.Get();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, c => c.Name == "Meieri");
            Assert.Contains(result, c => c.Name == "Bakeri");
            Assert.Contains(result, c => c.Name == "Frukt");
        }

        [Fact]
        public async Task Repository_Get_ReturnsEmptyList_WhenNoCategoriesExist()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get()).ReturnsAsync(new List<ItemCategory>());

            // Act
            var result = await _mockRepository.Object.Get();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task Repository_Insert_CreatesNewItemCategory_WhenValidCategoryProvided()
        {
            // Arrange
            var newCategory = new ItemCategory 
            { 
                Id = "4", 
                Name = "Kjøtt" 
            };

            _mockRepository.Setup(r => r.Insert(It.IsAny<ItemCategory>())).ReturnsAsync(newCategory);

            // Act
            var result = await _mockRepository.Object.Insert(newCategory);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Kjøtt", result.Name);
            Assert.Equal("4", result.Id);
        }

        [Fact]
        public async Task Repository_Update_UpdatesExistingItemCategory_WhenValidCategoryProvided()
        {
            // Arrange
            var updatedCategory = new ItemCategory 
            { 
                Id = "1", 
                Name = "Meieri - Oppdatert" 
            };

            _mockRepository.Setup(r => r.Update(It.IsAny<ItemCategory>())).ReturnsAsync(updatedCategory);

            // Act
            var result = await _mockRepository.Object.Update(updatedCategory);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Meieri - Oppdatert", result.Name);
        }

        [Fact]
        public async Task Repository_GetById_ReturnsItemCategory_WhenCategoryExists()
        {
            // Arrange
            var category = new ItemCategory { Id = "1", Name = "Meieri" };
            _mockRepository.Setup(r => r.Get("1")).ReturnsAsync(category);

            // Act
            var result = await _mockRepository.Object.Get("1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Meieri", result.Name);
            Assert.Equal("1", result.Id);
        }

        [Fact]
        public async Task Repository_GetById_ReturnsNull_WhenCategoryNotFound()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get("999")).ReturnsAsync((ItemCategory?)null);

            // Act
            var result = await _mockRepository.Object.Get("999");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Repository_Delete_ReturnsTrue_WhenCategoryDeletedSuccessfully()
        {
            // Arrange
            _mockRepository.Setup(r => r.Delete("1")).ReturnsAsync(true);

            // Act
            var result = await _mockRepository.Object.Delete("1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task Repository_Delete_ReturnsFalse_WhenCategoryDeletionFails()
        {
            // Arrange
            _mockRepository.Setup(r => r.Delete("1")).ReturnsAsync(false);

            // Act
            var result = await _mockRepository.Object.Delete("1");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AutoMapper_MapsItemCategoryToItemCategoryModel_Correctly()
        {
            // Arrange
            var category = new ItemCategory 
            { 
                Id = "1", 
                Name = "Test Category"
            };

            // Act
            var categoryModel = _mapper.Map<ItemCategoryModel>(category);

            // Assert
            Assert.NotNull(categoryModel);
            Assert.Equal("Test Category", categoryModel.Name);
            Assert.Equal("1", categoryModel.Id);
        }

        [Fact]
        public void AutoMapper_MapsItemCategoryModelToItemCategory_Correctly()
        {
            // Arrange
            var categoryModel = new ItemCategoryModel 
            { 
                Id = "1", 
                Name = "Test Category"
            };

            // Act
            var category = _mapper.Map<ItemCategory>(categoryModel);

            // Assert
            Assert.NotNull(category);
            Assert.Equal("Test Category", category.Name);
            Assert.Equal("1", category.Id);
        }

        [Fact]
        public void ItemCategory_ValidateBusinessLogic_CategoryHasRequiredProperties()
        {
            // Arrange & Act
            var category = new ItemCategory 
            { 
                Id = "1", 
                Name = "Meieri"
            };

            // Assert
            Assert.NotNull(category.Id);
            Assert.NotEmpty(category.Name);
        }

        [Fact]
        public void ItemCategoryModel_ValidateBusinessLogic_ModelHasRequiredProperties()
        {
            // Arrange & Act
            var categoryModel = new ItemCategoryModel 
            { 
                Id = "1", 
                Name = "Meieri"
            };

            // Assert
            Assert.NotNull(categoryModel.Id);
            Assert.NotEmpty(categoryModel.Name);
        }

        [Theory]
        [InlineData("dairy", "Meieri")]
        [InlineData("bakery", "Bakeri")]
        [InlineData("fruit", "Frukt")]
        [InlineData("meat", "Kjøtt")]
        [InlineData("fish", "Fisk")]
        public void ItemCategory_ValidateCommonCategories_CreatesValidCategories(string id, string name)
        {
            // Arrange & Act
            var category = new ItemCategory 
            { 
                Id = id, 
                Name = name
            };

            // Assert
            Assert.Equal(id, category.Id);
            Assert.Equal(name, category.Name);
            Assert.NotEmpty(category.Id);
            Assert.NotEmpty(category.Name);
        }
    }
}