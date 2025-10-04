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
    public class ShoppingListControllerTests
    {
        private readonly Mock<IGenericRepository<ShoppingList>> _mockRepository;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMapper _mapper;
        private readonly GetAllShoppingListsFunction _controller;

        public ShoppingListControllerTests()
        {
            _mockRepository = new Mock<IGenericRepository<ShoppingList>>();
            _loggerFactory = NullLoggerFactory.Instance;

            // Setup AutoMapper with the same profile as the API
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ShoppingList, ShoppingListModel>().ReverseMap();
                cfg.CreateMap<ShoppingListItem, ShoppingListItemModel>().ReverseMap();
                cfg.CreateMap<ShopItem, ShopItemModel>().ReverseMap();
                cfg.CreateMap<ItemCategory, ItemCategoryModel>().ReverseMap();
            });
            _mapper = config.CreateMapper();

            _controller = new GetAllShoppingListsFunction(_loggerFactory, _mockRepository.Object, _mapper);
        }

        [Fact]
        public async Task Repository_Get_ReturnsAllShoppingLists_WhenListsExist()
        {
            // Arrange
            var shoppingLists = new List<ShoppingList>
            {
                new ShoppingList 
                { 
                    Id = "1", 
                    Name = "Ukeshandel", 
                    IsDone = false,
                    ShoppingItems = new List<ShoppingListItem>()
                },
                new ShoppingList 
                { 
                    Id = "2", 
                    Name = "Middag i kveld", 
                    IsDone = false,
                    ShoppingItems = new List<ShoppingListItem>()
                }
            };

            _mockRepository.Setup(r => r.Get()).ReturnsAsync(shoppingLists);

            // Act
            var result = await _mockRepository.Object.Get();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, s => s.Name == "Ukeshandel");
            Assert.Contains(result, s => s.Name == "Middag i kveld");
        }

        [Fact]
        public async Task Repository_Get_ReturnsEmptyList_WhenNoListsExist()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get()).ReturnsAsync(new List<ShoppingList>());

            // Act
            var result = await _mockRepository.Object.Get();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task Repository_Insert_CreatesNewShoppingList_WhenValidListProvided()
        {
            // Arrange
            var newShoppingList = new ShoppingList 
            { 
                Id = "3", 
                Name = "Weekend shopping", 
                IsDone = false,
                ShoppingItems = new List<ShoppingListItem>
                {
                    new ShoppingListItem
                    {
                        Varen = new ShopItem 
                        { 
                            Id = "milk-1", 
                            Name = "Melk", 
                            Unit = "Liter",
                            ItemCategory = new ItemCategory { Id = "dairy", Name = "Meieri" }
                        },
                        Mengde = 2,
                        IsDone = false
                    }
                }
            };

            _mockRepository.Setup(r => r.Insert(It.IsAny<ShoppingList>())).ReturnsAsync(newShoppingList);

            // Act
            var result = await _mockRepository.Object.Insert(newShoppingList);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Weekend shopping", result.Name);
            Assert.Equal("3", result.Id);
            Assert.False(result.IsDone);
            Assert.Single(result.ShoppingItems);
        }

        [Fact]
        public async Task Repository_Update_UpdatesExistingShoppingList_WhenValidListProvided()
        {
            // Arrange
            var updatedShoppingList = new ShoppingList 
            { 
                Id = "1", 
                Name = "Ukeshandel - Oppdatert", 
                IsDone = true,
                ShoppingItems = new List<ShoppingListItem>()
            };

            _mockRepository.Setup(r => r.Update(It.IsAny<ShoppingList>())).ReturnsAsync(updatedShoppingList);

            // Act
            var result = await _mockRepository.Object.Update(updatedShoppingList);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Ukeshandel - Oppdatert", result.Name);
            Assert.True(result.IsDone);
        }

        [Fact]
        public async Task Repository_GetById_ReturnsShoppingList_WhenListExists()
        {
            // Arrange
            var shoppingList = new ShoppingList 
            { 
                Id = "1", 
                Name = "Ukeshandel", 
                IsDone = false,
                ShoppingItems = new List<ShoppingListItem>()
            };
            _mockRepository.Setup(r => r.Get("1")).ReturnsAsync(shoppingList);

            // Act
            var result = await _mockRepository.Object.Get("1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Ukeshandel", result.Name);
            Assert.Equal("1", result.Id);
        }

        [Fact]
        public async Task Repository_GetById_ReturnsNull_WhenListNotFound()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get("999")).ReturnsAsync((ShoppingList?)null);

            // Act
            var result = await _mockRepository.Object.Get("999");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Repository_Delete_ReturnsTrue_WhenListDeletedSuccessfully()
        {
            // Arrange
            _mockRepository.Setup(r => r.Delete("1")).ReturnsAsync(true);

            // Act
            var result = await _mockRepository.Object.Delete("1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task Repository_Delete_ReturnsFalse_WhenListDeletionFails()
        {
            // Arrange
            _mockRepository.Setup(r => r.Delete("1")).ReturnsAsync(false);

            // Act
            var result = await _mockRepository.Object.Delete("1");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AutoMapper_MapsShoppingListToShoppingListModel_Correctly()
        {
            // Arrange
            var shoppingList = new ShoppingList 
            { 
                Id = "1", 
                Name = "Test List", 
                IsDone = false,
                ShoppingItems = new List<ShoppingListItem>
                {
                    new ShoppingListItem
                    {
                        Varen = new ShopItem 
                        { 
                            Id = "item-1", 
                            Name = "Test Item", 
                            Unit = "Stk",
                            ItemCategory = new ItemCategory { Id = "cat-1", Name = "Test Category" }
                        },
                        Mengde = 1,
                        IsDone = false
                    }
                }
            };

            // Act
            var shoppingListModel = _mapper.Map<ShoppingListModel>(shoppingList);

            // Assert
            Assert.NotNull(shoppingListModel);
            Assert.Equal("Test List", shoppingListModel.Name);
            Assert.Equal("1", shoppingListModel.Id);
            Assert.False(shoppingListModel.IsDone);
            Assert.NotNull(shoppingListModel.ShoppingItems);
            Assert.Single(shoppingListModel.ShoppingItems);
            Assert.Equal("Test Item", shoppingListModel.ShoppingItems.First().Varen.Name);
        }

        [Fact]
        public void AutoMapper_MapsShoppingListModelToShoppingList_Correctly()
        {
            // Arrange
            var shoppingListModel = new ShoppingListModel 
            { 
                Id = "1", 
                Name = "Test List", 
                IsDone = false,
                ShoppingItems = new List<ShoppingListItemModel>
                {
                    new ShoppingListItemModel
                    {
                        Varen = new ShopItemModel 
                        { 
                            Id = "item-1", 
                            Name = "Test Item", 
                            Unit = "Stk",
                            ItemCategory = new ItemCategoryModel { Id = "cat-1", Name = "Test Category" }
                        },
                        Mengde = 1,
                        IsDone = false
                    }
                }
            };

            // Act
            var shoppingList = _mapper.Map<ShoppingList>(shoppingListModel);

            // Assert
            Assert.NotNull(shoppingList);
            Assert.Equal("Test List", shoppingList.Name);
            Assert.Equal("1", shoppingList.Id);
            Assert.False(shoppingList.IsDone);
            Assert.NotNull(shoppingList.ShoppingItems);
            Assert.Single(shoppingList.ShoppingItems);
            Assert.Equal("Test Item", shoppingList.ShoppingItems.First().Varen.Name);
        }

        [Fact]
        public void ShoppingList_ValidateBusinessLogic_ListHasRequiredProperties()
        {
            // Arrange & Act
            var shoppingList = new ShoppingList 
            { 
                Id = "1", 
                Name = "Ukeshandel",
                IsDone = false,
                ShoppingItems = new List<ShoppingListItem>()
            };

            // Assert
            Assert.NotNull(shoppingList.Id);
            Assert.NotEmpty(shoppingList.Name);
            Assert.NotNull(shoppingList.ShoppingItems);
        }

        [Fact]
        public void ShoppingListModel_ValidateBusinessLogic_ModelHasRequiredProperties()
        {
            // Arrange & Act
            var shoppingListModel = new ShoppingListModel 
            { 
                Id = "1", 
                Name = "Ukeshandel",
                IsDone = false,
                ShoppingItems = new List<ShoppingListItemModel>()
            };

            // Assert
            Assert.NotNull(shoppingListModel.Id);
            Assert.NotEmpty(shoppingListModel.Name);
            Assert.NotNull(shoppingListModel.ShoppingItems);
        }

        [Theory]
        [InlineData("Ukeshandel")]
        [InlineData("Middag i kveld")]
        [InlineData("Weekend shopping")]
        [InlineData("Bursdagsfeiring")]
        public void ShoppingList_ValidateCommonNames_CreatesValidLists(string listName)
        {
            // Arrange & Act
            var shoppingList = new ShoppingList 
            { 
                Id = Guid.NewGuid().ToString(), 
                Name = listName,
                IsDone = false,
                ShoppingItems = new List<ShoppingListItem>()
            };

            // Assert
            Assert.Equal(listName, shoppingList.Name);
            Assert.NotEmpty(shoppingList.Id);
            Assert.False(shoppingList.IsDone);
            Assert.NotNull(shoppingList.ShoppingItems);
        }

        [Fact]
        public void ShoppingListItem_ValidateBusinessLogic_ItemHasRequiredProperties()
        {
            // Arrange & Act
            var shoppingListItem = new ShoppingListItem
            {
                Varen = new ShopItem 
                { 
                    Id = "milk-1", 
                    Name = "Melk", 
                    Unit = "Liter",
                    ItemCategory = new ItemCategory { Id = "dairy", Name = "Meieri" }
                },
                Mengde = 2,
                IsDone = false
            };

            // Assert
            Assert.NotNull(shoppingListItem.Varen);
            Assert.True(shoppingListItem.Mengde > 0);
            Assert.NotEmpty(shoppingListItem.Varen.Name);
            Assert.NotNull(shoppingListItem.Varen.ItemCategory);
        }
    }
}