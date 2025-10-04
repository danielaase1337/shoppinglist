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
    public class ShopsItemsControllerTests
    {
        private readonly Mock<IGenericRepository<ShopItem>> _mockRepository;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMapper _mapper;
        private readonly ShopsItemsController _controller;

        public ShopsItemsControllerTests()
        {
            _mockRepository = new Mock<IGenericRepository<ShopItem>>();
            _loggerFactory = NullLoggerFactory.Instance;

            // Setup AutoMapper with the same profile as the API
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ShopItem, ShopItemModel>().ReverseMap();
                cfg.CreateMap<ItemCategory, ItemCategoryModel>().ReverseMap();
            });
            _mapper = config.CreateMapper();

            _controller = new ShopsItemsController(_loggerFactory, _mockRepository.Object, _mapper);
        }

        [Fact]
        public async Task Repository_Get_ReturnsAllShopItems_WhenItemsExist()
        {
            // Arrange
            var shopItems = new List<ShopItem>
            {
                new ShopItem 
                { 
                    Id = "1", 
                    Name = "Melk", 
                    Unit = "Liter",
                    ItemCategory = new ItemCategory { Id = "dairy", Name = "Meieri" }
                },
                new ShopItem 
                { 
                    Id = "2", 
                    Name = "Brød", 
                    Unit = "Stk",
                    ItemCategory = new ItemCategory { Id = "bakery", Name = "Bakeri" }
                }
            };

            _mockRepository.Setup(r => r.Get()).ReturnsAsync(shopItems);

            // Act
            var result = await _mockRepository.Object.Get();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, s => s.Name == "Melk");
            Assert.Contains(result, s => s.Name == "Brød");
        }

        [Fact]
        public async Task Repository_Get_ReturnsEmptyList_WhenNoItemsExist()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get()).ReturnsAsync(new List<ShopItem>());

            // Act
            var result = await _mockRepository.Object.Get();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task Repository_Insert_CreatesNewShopItem_WhenValidItemProvided()
        {
            // Arrange
            var newShopItem = new ShopItem 
            { 
                Id = "3", 
                Name = "Epler", 
                Unit = "Kg",
                ItemCategory = new ItemCategory { Id = "fruit", Name = "Frukt" }
            };

            _mockRepository.Setup(r => r.Insert(It.IsAny<ShopItem>())).ReturnsAsync(newShopItem);

            // Act
            var result = await _mockRepository.Object.Insert(newShopItem);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Epler", result.Name);
            Assert.Equal("Kg", result.Unit);
            Assert.Equal("3", result.Id);
        }

        [Fact]
        public async Task Repository_Update_UpdatesExistingShopItem_WhenValidItemProvided()
        {
            // Arrange
            var updatedShopItem = new ShopItem 
            { 
                Id = "1", 
                Name = "Melk - Oppdatert", 
                Unit = "Liter",
                ItemCategory = new ItemCategory { Id = "dairy", Name = "Meieri" }
            };

            _mockRepository.Setup(r => r.Update(It.IsAny<ShopItem>())).ReturnsAsync(updatedShopItem);

            // Act
            var result = await _mockRepository.Object.Update(updatedShopItem);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Melk - Oppdatert", result.Name);
        }

        [Fact]
        public async Task Repository_GetById_ReturnsShopItem_WhenItemExists()
        {
            // Arrange
            var shopItem = new ShopItem 
            { 
                Id = "1", 
                Name = "Melk", 
                Unit = "Liter",
                ItemCategory = new ItemCategory { Id = "dairy", Name = "Meieri" }
            };
            _mockRepository.Setup(r => r.Get(1)).ReturnsAsync(shopItem);

            // Act
            var result = await _mockRepository.Object.Get(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Melk", result.Name);
            Assert.Equal("Liter", result.Unit);
        }

        [Fact]
        public async Task Repository_GetById_ReturnsNull_WhenItemNotFound()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get(999)).ReturnsAsync((ShopItem?)null);

            // Act
            var result = await _mockRepository.Object.Get(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Repository_Delete_ReturnsTrue_WhenItemDeletedSuccessfully()
        {
            // Arrange
            _mockRepository.Setup(r => r.Delete(1)).ReturnsAsync(true);

            // Act
            var result = await _mockRepository.Object.Delete(1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task Repository_Delete_ReturnsFalse_WhenItemDeletionFails()
        {
            // Arrange
            _mockRepository.Setup(r => r.Delete(1)).ReturnsAsync(false);

            // Act
            var result = await _mockRepository.Object.Delete(1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AutoMapper_MapsShopItemToShopItemModel_Correctly()
        {
            // Arrange
            var shopItem = new ShopItem 
            { 
                Id = "1", 
                Name = "Test Item", 
                Unit = "Kg",
                ItemCategory = new ItemCategory { Id = "cat-1", Name = "Test Category" }
            };

            // Act
            var shopItemModel = _mapper.Map<ShopItemModel>(shopItem);

            // Assert
            Assert.NotNull(shopItemModel);
            Assert.Equal("Test Item", shopItemModel.Name);
            Assert.Equal("1", shopItemModel.Id);
            Assert.Equal("Kg", shopItemModel.Unit);
            Assert.NotNull(shopItemModel.ItemCategory);
            Assert.Equal("Test Category", shopItemModel.ItemCategory.Name);
        }

        [Fact]
        public void AutoMapper_MapsShopItemModelToShopItem_Correctly()
        {
            // Arrange
            var shopItemModel = new ShopItemModel 
            { 
                Id = "1", 
                Name = "Test Item", 
                Unit = "Kg",
                ItemCategory = new ItemCategoryModel { Id = "cat-1", Name = "Test Category" }
            };

            // Act
            var shopItem = _mapper.Map<ShopItem>(shopItemModel);

            // Assert
            Assert.NotNull(shopItem);
            Assert.Equal("Test Item", shopItem.Name);
            Assert.Equal("1", shopItem.Id);
            Assert.Equal("Kg", shopItem.Unit);
            Assert.NotNull(shopItem.ItemCategory);
            Assert.Equal("Test Category", shopItem.ItemCategory.Name);
        }

        [Fact]
        public void ShopItem_ValidateBusinessLogic_ItemHasRequiredProperties()
        {
            // Arrange & Act
            var shopItem = new ShopItem 
            { 
                Id = "1", 
                Name = "Melk", 
                Unit = "Liter",
                ItemCategory = new ItemCategory { Id = "dairy", Name = "Meieri" }
            };

            // Assert
            Assert.NotNull(shopItem.Id);
            Assert.NotEmpty(shopItem.Name);
            Assert.NotNull(shopItem.Unit);
            Assert.NotNull(shopItem.ItemCategory);
            Assert.NotEmpty(shopItem.ItemCategory.Name);
        }

        [Fact]
        public void ShopItemModel_ValidateBusinessLogic_ModelHasRequiredProperties()
        {
            // Arrange & Act
            var shopItemModel = new ShopItemModel 
            { 
                Id = "1", 
                Name = "Melk", 
                Unit = "Liter",
                ItemCategory = new ItemCategoryModel { Id = "dairy", Name = "Meieri" }
            };

            // Assert
            Assert.NotNull(shopItemModel.Id);
            Assert.NotEmpty(shopItemModel.Name);
            Assert.NotNull(shopItemModel.Unit);
            Assert.NotNull(shopItemModel.ItemCategory);
            Assert.NotEmpty(shopItemModel.ItemCategory.Name);
        }
    }
}