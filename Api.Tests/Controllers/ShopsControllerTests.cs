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
    public class ShopsControllerTests
    {
        private readonly Mock<IGenericRepository<Shop>> _mockRepository;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMapper _mapper;
        private readonly ShopsController _controller;

        public ShopsControllerTests()
        {
            _mockRepository = new Mock<IGenericRepository<Shop>>();
            _loggerFactory = NullLoggerFactory.Instance;

            // Setup AutoMapper with the same profile as the API
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Shop, ShopModel>().ReverseMap();
                cfg.CreateMap<Shelf, ShelfModel>().ReverseMap();
                cfg.CreateMap<ItemCategory, ItemCategoryModel>().ReverseMap();
            });
            _mapper = config.CreateMapper();

            _controller = new ShopsController(_loggerFactory, _mockRepository.Object, _mapper);
        }

        [Fact]
        public async Task Repository_Get_ReturnsAllShops_WhenShopsExist()
        {
            // Arrange
            var shops = new List<Shop>
            {
                new Shop { Id = "1", Name = "Rema 1000", ShelfsInShop = new List<Shelf>() },
                new Shop { Id = "2", Name = "ICA Maxi", ShelfsInShop = new List<Shelf>() }
            };

            _mockRepository.Setup(r => r.Get()).ReturnsAsync(shops);

            // Act
            var result = await _mockRepository.Object.Get();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, s => s.Name == "Rema 1000");
            Assert.Contains(result, s => s.Name == "ICA Maxi");
        }

        [Fact]
        public async Task Repository_Get_ReturnsEmptyList_WhenNoShopsExist()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get()).ReturnsAsync(new List<Shop>());

            // Act
            var result = await _mockRepository.Object.Get();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task Repository_Insert_CreatesNewShop_WhenValidShopProvided()
        {
            // Arrange
            var newShop = new Shop 
            { 
                Id = "3", 
                Name = "Kiwi", 
                ShelfsInShop = new List<Shelf>() 
            };

            _mockRepository.Setup(r => r.Insert(It.IsAny<Shop>())).ReturnsAsync(newShop);

            // Act
            var result = await _mockRepository.Object.Insert(newShop);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Kiwi", result.Name);
            Assert.Equal("3", result.Id);
        }

        [Fact]
        public async Task Repository_Update_UpdatesExistingShop_WhenValidShopProvided()
        {
            // Arrange
            var updatedShop = new Shop 
            { 
                Id = "1", 
                Name = "Rema 1000 - Updated", 
                ShelfsInShop = new List<Shelf>() 
            };

            _mockRepository.Setup(r => r.Update(It.IsAny<Shop>())).ReturnsAsync(updatedShop);

            // Act
            var result = await _mockRepository.Object.Update(updatedShop);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Rema 1000 - Updated", result.Name);
        }

        [Fact]
        public async Task Repository_GetById_ReturnsShop_WhenShopExists()
        {
            // Arrange
            var shop = new Shop { Id = "1", Name = "Rema 1000", ShelfsInShop = new List<Shelf>() };
            _mockRepository.Setup(r => r.Get(1)).ReturnsAsync(shop);

            // Act
            var result = await _mockRepository.Object.Get(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Rema 1000", result.Name);
        }

        [Fact]
        public async Task Repository_GetById_ReturnsNull_WhenShopNotFound()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get(999)).ReturnsAsync((Shop?)null);

            // Act
            var result = await _mockRepository.Object.Get(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Repository_Delete_ReturnsTrue_WhenShopDeletedSuccessfully()
        {
            // Arrange
            _mockRepository.Setup(r => r.Delete(1)).ReturnsAsync(true);

            // Act
            var result = await _mockRepository.Object.Delete(1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task Repository_Delete_ReturnsFalse_WhenShopDeletionFails()
        {
            // Arrange
            _mockRepository.Setup(r => r.Delete(1)).ReturnsAsync(false);

            // Act
            var result = await _mockRepository.Object.Delete(1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AutoMapper_MapsShopToShopModel_Correctly()
        {
            // Arrange
            var shop = new Shop 
            { 
                Id = "1", 
                Name = "Test Shop", 
                ShelfsInShop = new List<Shelf>
                {
                    new Shelf 
                    { 
                        Id = "shelf-1", 
                        Name = "Test Shelf", 
                        SortIndex = 1,
                        ItemCateogries = new List<ItemCategory>
                        {
                            new ItemCategory { Id = "cat-1", Name = "Test Category" }
                        }
                    }
                }
            };

            // Act
            var shopModel = _mapper.Map<ShopModel>(shop);

            // Assert
            Assert.NotNull(shopModel);
            Assert.Equal("Test Shop", shopModel.Name);
            Assert.Equal("1", shopModel.Id);
            Assert.NotNull(shopModel.ShelfsInShop);
            Assert.Single(shopModel.ShelfsInShop);
            Assert.Equal("Test Shelf", shopModel.ShelfsInShop.First().Name);
        }

        [Fact]
        public void AutoMapper_MapsShopModelToShop_Correctly()
        {
            // Arrange
            var shopModel = new ShopModel 
            { 
                Id = "1", 
                Name = "Test Shop", 
                ShelfsInShop = new List<ShelfModel>
                {
                    new ShelfModel 
                    { 
                        Id = "shelf-1", 
                        Name = "Test Shelf", 
                        SortIndex = 1,
                        ItemCateogries = new List<ItemCategoryModel>
                        {
                            new ItemCategoryModel { Id = "cat-1", Name = "Test Category" }
                        }
                    }
                }
            };

            // Act
            var shop = _mapper.Map<Shop>(shopModel);

            // Assert
            Assert.NotNull(shop);
            Assert.Equal("Test Shop", shop.Name);
            Assert.Equal("1", shop.Id);
            Assert.NotNull(shop.ShelfsInShop);
            Assert.Single(shop.ShelfsInShop);
            Assert.Equal("Test Shelf", shop.ShelfsInShop.First().Name);
        }
    }
}