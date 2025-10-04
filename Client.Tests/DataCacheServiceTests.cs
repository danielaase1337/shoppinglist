using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlazorApp.Client.Common;
using BlazorApp.Client.Services;
using Shared.HandlelisteModels;
using Moq;
using Moq.Protected;
using Xunit;

namespace Client.Tests.Services
{
    public class DataCacheServiceTests : IDisposable
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<ISettings> _settingsMock;
        private readonly DataCacheService _dataCache;

        public DataCacheServiceTests()
        {
            // Setup HttpClient mock
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://test.com/")
            };

            // Setup Settings mock
            _settingsMock = new Mock<ISettings>();
            _settingsMock.Setup(s => s.GetApiUrl(ShoppingListKeysEnum.shopItems))
                        .Returns("api/shopitems");
            _settingsMock.Setup(s => s.GetApiUrl(ShoppingListKeysEnum.itemcategorys))
                        .Returns("api/itemcategories");
            _settingsMock.Setup(s => s.GetApiUrl(ShoppingListKeysEnum.Shops))
                        .Returns("api/shops");
            _settingsMock.Setup(s => s.GetApiUrl(ShoppingListKeysEnum.ShoppingLists))
                        .Returns("api/shoppinglists");
            _settingsMock.Setup(s => s.GetApiUrlId(ShoppingListKeysEnum.ShoppingList, It.IsAny<string>()))
                        .Returns<ShoppingListKeysEnum, string>((_, id) => $"api/shoppinglist/{id}");
            _settingsMock.Setup(s => s.GetApiUrlId(ShoppingListKeysEnum.Shop, It.IsAny<string>()))
                        .Returns<ShoppingListKeysEnum, string>((_, id) => $"api/shop/{id}");

            // Create DataCacheService
            _dataCache = new DataCacheService(_httpClient, _settingsMock.Object);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #region Test Data Helpers

        private List<ShopItemModel> GetTestShopItems()
        {
            return new List<ShopItemModel>
            {
                new ShopItemModel 
                { 
                    Id = "1", 
                    Name = "Melk", 
                    Unit = "Liter",
                    ItemCategory = new ItemCategoryModel { Id = "cat1", Name = "Meieri" }
                },
                new ShopItemModel 
                { 
                    Id = "2", 
                    Name = "Brød", 
                    Unit = "Stk",
                    ItemCategory = new ItemCategoryModel { Id = "cat2", Name = "Bakevarer" }
                }
            };
        }

        private List<ItemCategoryModel> GetTestCategories()
        {
            return new List<ItemCategoryModel>
            {
                new ItemCategoryModel { Id = "cat1", Name = "Meieri" },
                new ItemCategoryModel { Id = "cat2", Name = "Bakevarer" },
                new ItemCategoryModel { Id = "cat3", Name = "Frukt og grønt" }
            };
        }

        private List<ShopModel> GetTestShops()
        {
            return new List<ShopModel>
            {
                new ShopModel { Id = "shop1", Name = "Rema 1000" },
                new ShopModel { Id = "shop2", Name = "Coop Prix" }
            };
        }

        private List<ShoppingListModel> GetTestShoppingLists()
        {
            return new List<ShoppingListModel>
            {
                new ShoppingListModel 
                { 
                    Id = "list1", 
                    Name = "Ukeshandel", 
                    IsDone = false,
                    ShoppingItems = new List<ShoppingListItemModel>()
                },
                new ShoppingListModel 
                { 
                    Id = "list2", 
                    Name = "Fest", 
                    IsDone = true,
                    ShoppingItems = new List<ShoppingListItemModel>()
                }
            };
        }

        private void SetupHttpResponse<T>(string url, T data, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var json = JsonSerializer.Serialize(data);
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains(url)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        #endregion

        #region GetItemsAsync Tests

        [Fact]
        public async Task GetItemsAsync_FirstCall_FetchesFromApiAndCaches()
        {
            // Arrange
            var testItems = GetTestShopItems();
            SetupHttpResponse("api/shopitems", testItems);

            // Act
            var result = await _dataCache.GetItemsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Melk", result.First().Name);

            // Verify HTTP call was made
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shopitems")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetItemsAsync_SecondCall_ReturnsCachedData()
        {
            // Arrange
            var testItems = GetTestShopItems();
            SetupHttpResponse("api/shopitems", testItems);

            // Act
            var firstResult = await _dataCache.GetItemsAsync();
            var secondResult = await _dataCache.GetItemsAsync();

            // Assert
            Assert.Same(firstResult, secondResult); // Should be same reference (cached)

            // Verify HTTP call was made only once
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shopitems")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetItemsAsync_ForceRefresh_FetchesFromApiAgain()
        {
            // Arrange
            var testItems = GetTestShopItems();
            SetupHttpResponse("api/shopitems", testItems);

            // Act
            await _dataCache.GetItemsAsync();
            var result = await _dataCache.GetItemsAsync(forceRefresh: true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            // Verify HTTP call was made twice
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shopitems")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetItemsAsync_HttpFailure_ReturnsEmptyList()
        {
            // Arrange
            SetupHttpResponse("api/shopitems", "", HttpStatusCode.InternalServerError);

            // Act
            var result = await _dataCache.GetItemsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetCategoriesAsync Tests

        [Fact]
        public async Task GetCategoriesAsync_FirstCall_FetchesFromApiAndCaches()
        {
            // Arrange
            var testCategories = GetTestCategories();
            SetupHttpResponse("api/itemcategories", testCategories);

            // Act
            var result = await _dataCache.GetCategoriesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, c => c.Name == "Meieri");
        }

        [Fact]
        public async Task GetCategoriesAsync_SecondCall_ReturnsCachedData()
        {
            // Arrange
            var testCategories = GetTestCategories();
            SetupHttpResponse("api/itemcategories", testCategories);

            // Act
            var firstResult = await _dataCache.GetCategoriesAsync();
            var secondResult = await _dataCache.GetCategoriesAsync();

            // Assert
            Assert.Same(firstResult, secondResult);

            // Verify HTTP call was made only once
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/itemcategories")),
                ItExpr.IsAny<CancellationToken>());
        }

        #endregion

        #region GetShopsAsync Tests

        [Fact]
        public async Task GetShopsAsync_FirstCall_FetchesFromApiAndCaches()
        {
            // Arrange
            var testShops = GetTestShops();
            SetupHttpResponse("api/shops", testShops);

            // Act
            var result = await _dataCache.GetShopsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, s => s.Name == "Rema 1000");
        }

        [Fact]
        public async Task GetShopsAsync_SecondCall_ReturnsCachedData()
        {
            // Arrange
            var testShops = GetTestShops();
            SetupHttpResponse("api/shops", testShops);

            // Act
            var firstResult = await _dataCache.GetShopsAsync();
            var secondResult = await _dataCache.GetShopsAsync();

            // Assert
            Assert.Same(firstResult, secondResult);
        }

        #endregion

        #region GetShoppingListsAsync Tests

        [Fact]
        public async Task GetShoppingListsAsync_FirstCall_FetchesFromApiAndCaches()
        {
            // Arrange
            var testLists = GetTestShoppingLists();
            SetupHttpResponse("api/shoppinglists", testLists);

            // Act
            var result = await _dataCache.GetShoppingListsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, l => l.Name == "Ukeshandel");
        }

        [Fact]
        public async Task GetShoppingListsAsync_SecondCall_ReturnsCachedData()
        {
            // Arrange
            var testLists = GetTestShoppingLists();
            SetupHttpResponse("api/shoppinglists", testLists);

            // Act
            var firstResult = await _dataCache.GetShoppingListsAsync();
            var secondResult = await _dataCache.GetShoppingListsAsync();

            // Assert
            Assert.Same(firstResult, secondResult);
        }

        #endregion

        #region GetShoppingListAsync Tests

        [Fact]
        public async Task GetShoppingListAsync_FirstCall_FetchesFromApiAndCaches()
        {
            // Arrange
            var testList = new ShoppingListModel 
            { 
                Id = "list1", 
                Name = "Test List",
                ShoppingItems = new List<ShoppingListItemModel>()
            };
            SetupHttpResponse("api/shoppinglist/list1", testList);

            // Act
            var result = await _dataCache.GetShoppingListAsync("list1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test List", result.Name);
            Assert.Equal("list1", result.Id);
        }

        [Fact]
        public async Task GetShoppingListAsync_SecondCall_ReturnsCachedData()
        {
            // Arrange
            var testList = new ShoppingListModel 
            { 
                Id = "list1", 
                Name = "Test List",
                ShoppingItems = new List<ShoppingListItemModel>()
            };
            SetupHttpResponse("api/shoppinglist/list1", testList);

            // Act
            var firstResult = await _dataCache.GetShoppingListAsync("list1");
            var secondResult = await _dataCache.GetShoppingListAsync("list1");

            // Assert
            Assert.Same(firstResult, secondResult);

            // Verify HTTP call was made only once
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shoppinglist/list1")),
                ItExpr.IsAny<CancellationToken>());
        }

        #endregion

        #region GetShopAsync Tests

        [Fact]
        public async Task GetShopAsync_FirstCall_FetchesFromApiAndCaches()
        {
            // Arrange
            var testShop = new ShopModel { Id = "shop1", Name = "Test Shop" };
            SetupHttpResponse("api/shop/shop1", testShop);

            // Act
            var result = await _dataCache.GetShopAsync("shop1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Shop", result.Name);
            Assert.Equal("shop1", result.Id);
        }

        [Fact]
        public async Task GetShopAsync_SecondCall_ReturnsCachedData()
        {
            // Arrange
            var testShop = new ShopModel { Id = "shop1", Name = "Test Shop" };
            SetupHttpResponse("api/shop/shop1", testShop);

            // Act
            var firstResult = await _dataCache.GetShopAsync("shop1");
            var secondResult = await _dataCache.GetShopAsync("shop1");

            // Assert
            Assert.Same(firstResult, secondResult);
        }

        #endregion

        #region Cache Invalidation Tests

        [Fact]
        public async Task InvalidateItemsCache_ClearsItemsCache()
        {
            // Arrange
            var testItems = GetTestShopItems();
            SetupHttpResponse("api/shopitems", testItems);

            // Act
            await _dataCache.GetItemsAsync(); // Cache items
            _dataCache.InvalidateItemsCache(); // Invalidate cache
            await _dataCache.GetItemsAsync(); // Should fetch again

            // Assert
            // Verify HTTP call was made twice
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shopitems")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task InvalidateCategoriesCache_ClearsCategoriesCache()
        {
            // Arrange
            var testCategories = GetTestCategories();
            SetupHttpResponse("api/itemcategories", testCategories);

            // Act
            await _dataCache.GetCategoriesAsync(); // Cache categories
            _dataCache.InvalidateCategoriesCache(); // Invalidate cache
            await _dataCache.GetCategoriesAsync(); // Should fetch again

            // Assert
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/itemcategories")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task InvalidateShopsCache_ClearsShopsCache()
        {
            // Arrange
            var testShops = GetTestShops();
            SetupHttpResponse("api/shops", testShops);

            // Act
            await _dataCache.GetShopsAsync(); // Cache shops
            _dataCache.InvalidateShopsCache(); // Invalidate cache
            await _dataCache.GetShopsAsync(); // Should fetch again

            // Assert
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shops")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task InvalidateShoppingListsCache_ClearsShoppingListsCache()
        {
            // Arrange
            var testLists = GetTestShoppingLists();
            SetupHttpResponse("api/shoppinglists", testLists);

            // Act
            await _dataCache.GetShoppingListsAsync(); // Cache lists
            _dataCache.InvalidateShoppingListsCache(); // Invalidate cache
            await _dataCache.GetShoppingListsAsync(); // Should fetch again

            // Assert
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shoppinglists")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task InvalidateShoppingListCache_ClearsSpecificShoppingListCache()
        {
            // Arrange
            var testList = new ShoppingListModel 
            { 
                Id = "list1", 
                Name = "Test List",
                ShoppingItems = new List<ShoppingListItemModel>()
            };
            SetupHttpResponse("api/shoppinglist/list1", testList);

            // Act
            await _dataCache.GetShoppingListAsync("list1"); // Cache specific list
            _dataCache.InvalidateShoppingListCache("list1"); // Invalidate specific cache
            await _dataCache.GetShoppingListAsync("list1"); // Should fetch again

            // Assert
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shoppinglist/list1")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task InvalidateShopCache_ClearsSpecificShopCache()
        {
            // Arrange
            var testShop = new ShopModel { Id = "shop1", Name = "Test Shop" };
            SetupHttpResponse("api/shop/shop1", testShop);

            // Act
            await _dataCache.GetShopAsync("shop1"); // Cache specific shop
            _dataCache.InvalidateShopCache("shop1"); // Invalidate specific cache
            await _dataCache.GetShopAsync("shop1"); // Should fetch again

            // Assert
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shop/shop1")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task InvalidateAllCaches_ClearsAllCachedData()
        {
            // Arrange
            SetupHttpResponse("api/shopitems", GetTestShopItems());
            SetupHttpResponse("api/itemcategories", GetTestCategories());
            SetupHttpResponse("api/shops", GetTestShops());
            SetupHttpResponse("api/shoppinglists", GetTestShoppingLists());

            // Act - Cache all data
            await _dataCache.GetItemsAsync();
            await _dataCache.GetCategoriesAsync();
            await _dataCache.GetShopsAsync();
            await _dataCache.GetShoppingListsAsync();

            // Clear all caches
            _dataCache.InvalidateAllCaches();

            // Fetch again - should make new HTTP calls
            await _dataCache.GetItemsAsync();
            await _dataCache.GetCategoriesAsync();
            await _dataCache.GetShopsAsync();
            await _dataCache.GetShoppingListsAsync();

            // Assert - Each endpoint should have been called twice
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shopitems")),
                ItExpr.IsAny<CancellationToken>());

            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/itemcategories")),
                ItExpr.IsAny<CancellationToken>());

            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shops")),
                ItExpr.IsAny<CancellationToken>());

            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shoppinglists")),
                ItExpr.IsAny<CancellationToken>());
        }

        #endregion

        #region PreloadActiveShoppingListsAsync Tests

        [Fact]
        public async Task PreloadActiveShoppingListsAsync_PreloadsOnlyActiveShoppingLists()
        {
            // Arrange
            var testLists = GetTestShoppingLists(); // Contains one active (IsDone=false) and one completed (IsDone=true)
            var activeList = new ShoppingListModel 
            { 
                Id = "list1", 
                Name = "Ukeshandel", 
                IsDone = false,
                ShoppingItems = new List<ShoppingListItemModel>()
            };

            SetupHttpResponse("api/shoppinglists", testLists);
            SetupHttpResponse("api/shoppinglist/list1", activeList);

            // Act
            await _dataCache.PreloadActiveShoppingListsAsync();

            // Assert
            // Should have fetched the shopping lists
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shoppinglists")),
                ItExpr.IsAny<CancellationToken>());

            // Should have preloaded only the active list (list1), not the completed one (list2)
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shoppinglist/list1")),
                ItExpr.IsAny<CancellationToken>());

            // Should NOT have preloaded the completed list (list2)
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shoppinglist/list2")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task PreloadActiveShoppingListsAsync_NoActiveLists_DoesNotPreloadAnything()
        {
            // Arrange
            var completedLists = new List<ShoppingListModel>
            {
                new ShoppingListModel { Id = "list1", Name = "Completed 1", IsDone = true },
                new ShoppingListModel { Id = "list2", Name = "Completed 2", IsDone = true }
            };
            SetupHttpResponse("api/shoppinglists", completedLists);

            // Act
            await _dataCache.PreloadActiveShoppingListsAsync();

            // Assert
            // Should have fetched the shopping lists to check for active ones
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shoppinglists")),
                ItExpr.IsAny<CancellationToken>());

            // Should NOT have preloaded any individual lists since none are active
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shoppinglist/")),
                ItExpr.IsAny<CancellationToken>());
        }

        #endregion

        #region PreloadCoreDataAsync Tests

        [Fact]
        public async Task PreloadCoreDataAsync_PreloadsAllCoreDataInParallel()
        {
            // Arrange
            SetupHttpResponse("api/shopitems", GetTestShopItems());
            SetupHttpResponse("api/itemcategories", GetTestCategories());
            SetupHttpResponse("api/shops", GetTestShops());
            SetupHttpResponse("api/shoppinglists", GetTestShoppingLists());

            // Act
            await _dataCache.PreloadCoreDataAsync();

            // Assert - All core endpoints should have been called once
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shopitems")),
                ItExpr.IsAny<CancellationToken>());

            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/itemcategories")),
                ItExpr.IsAny<CancellationToken>());

            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shops")),
                ItExpr.IsAny<CancellationToken>());

            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shoppinglists")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task PreloadCoreDataAsync_AfterPreload_SubsequentCallsUseCachedData()
        {
            // Arrange
            SetupHttpResponse("api/shopitems", GetTestShopItems());
            SetupHttpResponse("api/itemcategories", GetTestCategories());
            SetupHttpResponse("api/shops", GetTestShops());
            SetupHttpResponse("api/shoppinglists", GetTestShoppingLists());

            // Act
            await _dataCache.PreloadCoreDataAsync();

            // Now get data individually - should use cached data
            var items = await _dataCache.GetItemsAsync();
            var categories = await _dataCache.GetCategoriesAsync();
            var shops = await _dataCache.GetShopsAsync();
            var lists = await _dataCache.GetShoppingListsAsync();

            // Assert
            Assert.NotNull(items);
            Assert.NotNull(categories);
            Assert.NotNull(shops);
            Assert.NotNull(lists);

            // All endpoints should still have been called only once (from preload)
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shopitems")),
                ItExpr.IsAny<CancellationToken>());

            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/itemcategories")),
                ItExpr.IsAny<CancellationToken>());

            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shops")),
                ItExpr.IsAny<CancellationToken>());

            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/shoppinglists")),
                ItExpr.IsAny<CancellationToken>());
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task GetItemsAsync_NetworkError_ReturnsEmptyListAndDoesNotCache()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await _dataCache.GetItemsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            // Subsequent call should try API again (nothing was cached)
            var secondResult = await _dataCache.GetItemsAsync();
            Assert.NotNull(secondResult);
            Assert.Empty(secondResult);

            // Verify two attempts were made
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task PreloadCoreDataAsync_PartialFailure_ContinuesWithOtherEndpoints()
        {
            // Arrange
            SetupHttpResponse("api/shopitems", GetTestShopItems());
            SetupHttpResponse("api/itemcategories", "", HttpStatusCode.InternalServerError); // This fails
            SetupHttpResponse("api/shops", GetTestShops());
            SetupHttpResponse("api/shoppinglists", GetTestShoppingLists());

            // Act & Assert - Should not throw exception
            await _dataCache.PreloadCoreDataAsync();

            // Items should be cached successfully
            var items = await _dataCache.GetItemsAsync();
            Assert.NotNull(items);
            Assert.Equal(2, items.Count);

            // Categories should return empty list due to failure
            var categories = await _dataCache.GetCategoriesAsync();
            Assert.NotNull(categories);
            Assert.Empty(categories);

            // Shops should be cached successfully
            var shops = await _dataCache.GetShopsAsync();
            Assert.NotNull(shops);
            Assert.Equal(2, shops.Count);
        }

        #endregion
    }
}