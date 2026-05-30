using Api.Controllers;
using Api.Tests.Helpers;
using AutoMapper;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared;
using Shared.FireStoreDataModels;
using Shared.HandlelisteModels;
using Shared.Repository;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Api.Tests.Controllers
{
    /// <summary>
    /// Unit tests for InventoryItemController.
    /// All tests call actual controller methods (RunAll / RunOne / RunAdjust) — not mocks directly.
    ///
    /// ⚠️  Key Moq reminder: For Task&lt;ICollection&lt;T&gt;&gt; return types use:
    ///       .Returns(Task.FromResult&lt;ICollection&lt;InventoryItem&gt;&gt;(list))
    ///     NOT .ReturnsAsync(list) — that silently returns null at runtime.
    ///
    /// InventoryAdjustmentModel is a nested class in the Api.Controllers namespace.
    /// </summary>
    public class InventoryItemControllerTests
    {
        private readonly Mock<IGenericRepository<InventoryItem>> _mockRepository;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMapper _mapper;
        private readonly InventoryItemController _controller;

        private static readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public InventoryItemControllerTests()
        {
            _mockRepository = new Mock<IGenericRepository<InventoryItem>>();
            _loggerFactory = NullLoggerFactory.Instance;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<InventoryItem, InventoryItemModel>().ReverseMap();
            });
            _mapper = config.CreateMapper();

            _controller = new InventoryItemController(_loggerFactory, _mockRepository.Object, _mapper);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────

        private static async Task<T?> ReadBody<T>(HttpResponseData response)
        {
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            return JsonSerializer.Deserialize<T>(body, _jsonOptions);
        }

        private static List<InventoryItem> CreateSampleItems() => new List<InventoryItem>
        {
            new InventoryItem
            {
                Id = "inv-1",
                Name = "Melk",
                ShopItemId = "shop-melk",
                ShopItemName = "Melk",
                QuantityInStock = 2.0,
                Unit = MealUnit.Liter,
                IsActive = true,
                LastModified = DateTime.UtcNow.AddDays(-3)
            },
            new InventoryItem
            {
                Id = "inv-2",
                Name = "Brød",
                ShopItemId = "shop-brod",
                ShopItemName = "Brød",
                QuantityInStock = 1.0,
                Unit = MealUnit.Piece,
                IsActive = true,
                LastModified = DateTime.UtcNow.AddDays(-1)
            },
            new InventoryItem
            {
                Id = "inv-3",
                Name = "Archived Item",
                ShopItemId = "shop-archived",
                ShopItemName = "Archived",
                QuantityInStock = 5.0,
                Unit = MealUnit.Piece,
                IsActive = false,  // soft-deleted — should not appear in GET
                LastModified = DateTime.UtcNow.AddDays(-10)
            }
        };

        // ── Test 1: GetAll returns active items only, ordered by Name ─────────────────

        [Fact]
        public async Task GetAll_ReturnsActiveItems()
        {
            // Arrange: 3 items — 2 active, 1 inactive (soft-deleted)
            var items = CreateSampleItems();
            _mockRepository.Setup(r => r.Get())
                .Returns(Task.FromResult<ICollection<InventoryItem>>(items));
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/inventoryitems");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<InventoryItemModel[]>(response);

            // Assert: only 2 active items returned
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(2, result.Length);
            Assert.All(result, item => Assert.True(item.IsActive));

            // Verify ordering: Name ascending — "Brød" before "Melk"
            Assert.Equal("Brød", result[0].Name);
            Assert.Equal("Melk", result[1].Name);
        }

        // ── Test 2: GetAll returns empty list when no items ───────────────────────────

        [Fact]
        public async Task GetAll_ReturnsEmptyList_WhenNone()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get())
                .Returns(Task.FromResult<ICollection<InventoryItem>>(new List<InventoryItem>()));
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/inventoryitems");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<InventoryItemModel[]>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // ── Test 3: GetAll returns 500 when repository returns null ──────────────────

        [Fact]
        public async Task GetAll_ReturnsError_WhenRepositoryReturnsNull()
        {
            // Arrange: repository returns null (simulates Firestore failure)
            _mockRepository.Setup(r => r.Get())
                .Returns(Task.FromResult<ICollection<InventoryItem>>(null!));
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/inventoryitems");

            // Act
            var response = await _controller.RunAll(req);

            // Assert: ControllerBase.GetErroRespons returns 500
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ── Test 4: POST sets IsActive=true and LastModified ─────────────────────────

        [Fact]
        public async Task Create_SetsIsActiveAndLastModified()
        {
            // Arrange: client sends IsActive=false — controller should override to true
            var newItemModel = new InventoryItemModel
            {
                Id = "inv-new",
                Name = "Egg",
                ShopItemId = "shop-egg",
                ShopItemName = "Egg",
                QuantityInStock = 0,
                IsActive = false  // controller must set this to true
            };

            InventoryItem? capturedItem = null;
            _mockRepository
                .Setup(r => r.Insert(It.IsAny<InventoryItem>()))
                .Callback<InventoryItem>(item => capturedItem = item)
                .ReturnsAsync((InventoryItem item) =>
                {
                    item.Id = "inv-new";
                    return item;
                });

            var req = TestHttpFactory.CreatePostRequest(
                JsonSerializer.Serialize(newItemModel),
                "http://localhost/api/inventoryitems");

            // Act
            var response = await _controller.RunAll(req);

            // Assert: controller sets both IsActive=true and LastModified before Insert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(capturedItem);
            Assert.True(capturedItem!.IsActive);
            Assert.NotNull(capturedItem.LastModified);

            _mockRepository.Verify(r => r.Insert(It.Is<InventoryItem>(
                item => item.IsActive && item.LastModified.HasValue
            )), Times.Once);
        }

        // ── Test 5: POST returns created model ────────────────────────────────────────

        [Fact]
        public async Task Create_ReturnsCreatedModel()
        {
            // Arrange
            var newItemModel = new InventoryItemModel
            {
                Name = "Smør",
                ShopItemId = "shop-smor",
                ShopItemName = "Smør",
                QuantityInStock = 0
            };

            var savedItem = new InventoryItem
            {
                Id = "inv-smor",
                Name = "Smør",
                ShopItemId = "shop-smor",
                ShopItemName = "Smør",
                QuantityInStock = 0,
                IsActive = true,
                LastModified = DateTime.UtcNow
            };

            _mockRepository.Setup(r => r.Insert(It.IsAny<InventoryItem>())).ReturnsAsync(savedItem);

            var req = TestHttpFactory.CreatePostRequest(
                JsonSerializer.Serialize(newItemModel),
                "http://localhost/api/inventoryitems");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<InventoryItemModel>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal("inv-smor", result.Id);
            Assert.Equal("Smør", result.Name);
            Assert.True(result.IsActive);
        }

        // ── Test 6: PUT updates LastModified ──────────────────────────────────────────

        [Fact]
        public async Task Update_SetsLastModified()
        {
            // Arrange
            var oldTimestamp = DateTime.UtcNow.AddDays(-5);
            var existingModel = new InventoryItemModel
            {
                Id = "inv-1",
                Name = "Melk",
                ShopItemId = "shop-melk",
                ShopItemName = "Melk",
                QuantityInStock = 3.0,
                IsActive = true,
                LastModified = oldTimestamp
            };

            var updatedItem = _mapper.Map<InventoryItem>(existingModel);
            updatedItem.LastModified = DateTime.UtcNow;

            _mockRepository
                .Setup(r => r.Update(It.IsAny<InventoryItem>()))
                .ReturnsAsync(updatedItem);

            var req = TestHttpFactory.CreatePutRequest(
                JsonSerializer.Serialize(existingModel),
                "http://localhost/api/inventoryitems");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<InventoryItemModel>(response);

            // Assert: controller set a newer LastModified before calling Update
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotNull(result.LastModified);

            _mockRepository.Verify(r => r.Update(It.Is<InventoryItem>(
                item => item.LastModified.HasValue && item.LastModified.Value > oldTimestamp
            )), Times.Once);
        }

        // ── Test 7: GET /{id} returns item when found ─────────────────────────────────

        [Fact]
        public async Task GetById_ReturnsItem_WhenFound()
        {
            // Arrange
            var item = new InventoryItem
            {
                Id = "inv-1",
                Name = "Melk",
                ShopItemId = "shop-melk",
                ShopItemName = "Melk",
                QuantityInStock = 2.0,
                IsActive = true
            };
            _mockRepository.Setup(r => r.Get("inv-1")).ReturnsAsync(item);
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/inventoryitem/inv-1");

            // Act
            var response = await _controller.RunOne(req, "inv-1");
            var result = await ReadBody<InventoryItemModel>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal("inv-1", result.Id);
            Assert.Equal("Melk", result.Name);
            Assert.Equal(2.0, result.QuantityInStock);
        }

        // ── Test 8: GET /{id} returns 404 when not found ──────────────────────────────

        [Fact]
        public async Task GetById_Returns404_WhenNotFound()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get("nonexistent")).ReturnsAsync((InventoryItem?)null);
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/inventoryitem/nonexistent");

            // Act
            var response = await _controller.RunOne(req, "nonexistent");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ── Test 9: DELETE performs soft delete — sets IsActive=false ─────────────────

        [Fact]
        public async Task Delete_SoftDeletes_SetsIsActiveFalse()
        {
            // Arrange
            var existingItem = new InventoryItem
            {
                Id = "inv-1",
                Name = "Melk",
                ShopItemId = "shop-melk",
                ShopItemName = "Melk",
                QuantityInStock = 2.0,
                IsActive = true
            };

            var softDeletedItem = new InventoryItem
            {
                Id = "inv-1",
                Name = "Melk",
                ShopItemId = "shop-melk",
                ShopItemName = "Melk",
                QuantityInStock = 2.0,
                IsActive = false,
                LastModified = DateTime.UtcNow
            };

            _mockRepository.Setup(r => r.Get("inv-1")).ReturnsAsync(existingItem);
            _mockRepository
                .Setup(r => r.Update(It.Is<InventoryItem>(i => !i.IsActive)))
                .ReturnsAsync(softDeletedItem);

            var req = TestHttpFactory.CreateDeleteRequest("http://localhost/api/inventoryitem/inv-1");

            // Act
            var response = await _controller.RunOne(req, "inv-1");
            var result = await ReadBody<InventoryItemModel>(response);

            // Assert: returns 200 with the soft-deleted item
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.False(result.IsActive);

            // Verify soft-delete pattern: Update called with IsActive=false
            _mockRepository.Verify(r => r.Update(It.Is<InventoryItem>(
                i => !i.IsActive && i.LastModified.HasValue
            )), Times.Once);
        }

        // ── Test 10: DELETE never calls _repository.Delete() ─────────────────────────

        [Fact]
        public async Task Delete_DoesNotCallRepositoryDelete()
        {
            // Arrange
            var existingItem = new InventoryItem
            {
                Id = "inv-1",
                Name = "Melk",
                ShopItemId = "shop-melk",
                ShopItemName = "Melk",
                IsActive = true
            };
            var softDeletedItem = new InventoryItem { Id = "inv-1", IsActive = false, LastModified = DateTime.UtcNow };

            _mockRepository.Setup(r => r.Get("inv-1")).ReturnsAsync(existingItem);
            _mockRepository.Setup(r => r.Update(It.IsAny<InventoryItem>())).ReturnsAsync(softDeletedItem);

            var req = TestHttpFactory.CreateDeleteRequest("http://localhost/api/inventoryitem/inv-1");

            // Act
            await _controller.RunOne(req, "inv-1");

            // Assert: hard Delete was NEVER called — only soft delete via Update
            _mockRepository.Verify(r => r.Delete(It.IsAny<object>()), Times.Never);
        }

        // ── Test 11: DELETE returns 404 when item not found ───────────────────────────

        [Fact]
        public async Task Delete_Returns404_WhenNotFound()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get("nonexistent")).ReturnsAsync((InventoryItem?)null);
            var req = TestHttpFactory.CreateDeleteRequest("http://localhost/api/inventoryitem/nonexistent");

            // Act
            var response = await _controller.RunOne(req, "nonexistent");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ── Test 12: Adjust increases quantity ────────────────────────────────────────

        [Fact]
        public async Task Adjust_IncreasesQuantity()
        {
            // Arrange: item has 3.0 stock; delta = +5 → expected 8.0
            var item = new InventoryItem
            {
                Id = "inv-1",
                Name = "Melk",
                ShopItemId = "shop-melk",
                ShopItemName = "Melk",
                QuantityInStock = 3.0,
                IsActive = true
            };

            _mockRepository.Setup(r => r.Get())
                .Returns(Task.FromResult<ICollection<InventoryItem>>(new List<InventoryItem> { item }));
            _mockRepository.Setup(r => r.Update(It.IsAny<InventoryItem>()))
                .ReturnsAsync((InventoryItem i) => i);

            var adjustments = new List<InventoryAdjustmentModel>
            {
                new InventoryAdjustmentModel { Id = "inv-1", QuantityDelta = 5.0 }
            };

            var req = TestHttpFactory.CreatePostRequest(
                JsonSerializer.Serialize(adjustments),
                "http://localhost/api/inventoryitems/adjust");

            // Act
            var response = await _controller.RunAdjust(req);
            var result = await ReadBody<InventoryItemModel[]>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(8.0, result[0].QuantityInStock);

            _mockRepository.Verify(r => r.Update(It.Is<InventoryItem>(
                i => i.Id == "inv-1" && i.QuantityInStock == 8.0
            )), Times.Once);
        }

        // ── Test 13: Adjust decreases quantity ────────────────────────────────────────

        [Fact]
        public async Task Adjust_DecreasesQuantity()
        {
            // Arrange: item has 5.0 stock; delta = -3 → expected 2.0
            var item = new InventoryItem
            {
                Id = "inv-2",
                Name = "Brød",
                ShopItemId = "shop-brod",
                ShopItemName = "Brød",
                QuantityInStock = 5.0,
                IsActive = true
            };

            _mockRepository.Setup(r => r.Get())
                .Returns(Task.FromResult<ICollection<InventoryItem>>(new List<InventoryItem> { item }));
            _mockRepository.Setup(r => r.Update(It.IsAny<InventoryItem>()))
                .ReturnsAsync((InventoryItem i) => i);

            var adjustments = new List<InventoryAdjustmentModel>
            {
                new InventoryAdjustmentModel { Id = "inv-2", QuantityDelta = -3.0 }
            };

            var req = TestHttpFactory.CreatePostRequest(
                JsonSerializer.Serialize(adjustments),
                "http://localhost/api/inventoryitems/adjust");

            // Act
            var response = await _controller.RunAdjust(req);
            var result = await ReadBody<InventoryItemModel[]>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(2.0, result[0].QuantityInStock);
        }

        // ── Test 14: Adjust clamps to zero when delta exceeds stock ──────────────────

        [Fact]
        public async Task Adjust_ClampsToZero_WhenDeltaExceedsStock()
        {
            // Arrange: item has 3.0 stock; delta = -10 → floor at 0.0 (not -7)
            var item = new InventoryItem
            {
                Id = "inv-3",
                Name = "Egg",
                ShopItemId = "shop-egg",
                ShopItemName = "Egg",
                QuantityInStock = 3.0,
                IsActive = true
            };

            _mockRepository.Setup(r => r.Get())
                .Returns(Task.FromResult<ICollection<InventoryItem>>(new List<InventoryItem> { item }));
            _mockRepository.Setup(r => r.Update(It.IsAny<InventoryItem>()))
                .ReturnsAsync((InventoryItem i) => i);

            var adjustments = new List<InventoryAdjustmentModel>
            {
                new InventoryAdjustmentModel { Id = "inv-3", QuantityDelta = -10.0 }
            };

            var req = TestHttpFactory.CreatePostRequest(
                JsonSerializer.Serialize(adjustments),
                "http://localhost/api/inventoryitems/adjust");

            // Act
            var response = await _controller.RunAdjust(req);
            var result = await ReadBody<InventoryItemModel[]>(response);

            // Assert: quantity must be clamped at 0, not negative
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(0.0, result[0].QuantityInStock);

            _mockRepository.Verify(r => r.Update(It.Is<InventoryItem>(
                i => i.Id == "inv-3" && i.QuantityInStock == 0.0
            )), Times.Once);
        }
    }
}
