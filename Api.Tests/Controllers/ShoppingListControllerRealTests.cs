using Api.Controllers;
using Api.Tests.Helpers;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.FireStoreDataModels;
using Shared.HandlelisteModels;
using Shared.Repository;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Api.Tests.Controllers
{
    /// <summary>
    /// Real controller tests — instantiate actual GetAllShoppingListsFunction with mocked dependencies
    /// and call controller methods directly. Established as Sprint 0 fix for issue #19.
    /// </summary>
    public class ShoppingListControllerRealTests
    {
        private readonly Mock<IGenericRepository<ShoppingList>> _mockRepo;
        private readonly IMapper _mapper;
        private readonly GetAllShoppingListsFunction _controller;

        public ShoppingListControllerRealTests()
        {
            _mockRepo = new Mock<IGenericRepository<ShoppingList>>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ShoppingList, ShoppingListModel>().ReverseMap();
                cfg.CreateMap<ShoppingListItem, ShoppingListItemModel>().ReverseMap();
                cfg.CreateMap<ShopItem, ShopItemModel>().ReverseMap();
                cfg.CreateMap<ItemCategory, ItemCategoryModel>().ReverseMap();
            });
            _mapper = config.CreateMapper();

            _controller = new GetAllShoppingListsFunction(NullLoggerFactory.Instance, _mockRepo.Object, _mapper);
        }

        // ─── GET all ────────────────────────────────────────────────────────────────

        [Fact]
        public async Task RunAll_GET_ReturnsOk_WhenListsExist()
        {
            var lists = new List<ShoppingList>
            {
                new ShoppingList { Id = "1", Name = "Ukeshandel", LastModified = DateTime.UtcNow, ShoppingItems = new List<ShoppingListItem>() },
                new ShoppingList { Id = "2", Name = "Middag", LastModified = DateTime.UtcNow, ShoppingItems = new List<ShoppingListItem>() }
            };
            _mockRepo.Setup(r => r.Get()).ReturnsAsync(lists);
            _mockRepo.Setup(r => r.Update(It.IsAny<ShoppingList>())).ReturnsAsync((ShoppingList sl) => sl);

            var request = TestHttpFactory.CreateGetRequest();
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Ukeshandel", body);
            Assert.Contains("Middag", body);
        }

        [Fact]
        public async Task RunAll_GET_ReturnsOkWithEmptyArray_WhenNoListsExist()
        {
            _mockRepo.Setup(r => r.Get()).ReturnsAsync(new List<ShoppingList>());

            var request = TestHttpFactory.CreateGetRequest();
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Equal("[]", body.Trim());
        }

        [Fact]
        public async Task RunAll_GET_ReturnsInternalServerError_WhenRepositoryReturnsNull()
        {
            _mockRepo.Setup(r => r.Get()).ReturnsAsync((List<ShoppingList>?)null!);

            var request = TestHttpFactory.CreateGetRequest();
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task RunAll_GET_MigratesLegacyLists_WhenLastModifiedIsNull()
        {
            var legacyList = new ShoppingList { Id = "old-1", Name = "Legacy List", LastModified = null, ShoppingItems = new List<ShoppingListItem>() };
            _mockRepo.Setup(r => r.Get()).ReturnsAsync(new List<ShoppingList> { legacyList });
            _mockRepo.Setup(r => r.Update(It.IsAny<ShoppingList>())).ReturnsAsync((ShoppingList sl) => sl);

            var request = TestHttpFactory.CreateGetRequest();
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            // Migration should have called Update for the list without LastModified
            _mockRepo.Verify(r => r.Update(It.Is<ShoppingList>(sl => sl.Id == "old-1" && sl.LastModified.HasValue)), Times.Once);
        }

        [Fact]
        public async Task RunAll_GET_DoesNotCallUpdate_WhenAllListsHaveLastModified()
        {
            var lists = new List<ShoppingList>
            {
                new ShoppingList { Id = "1", Name = "List 1", LastModified = DateTime.UtcNow, ShoppingItems = new List<ShoppingListItem>() }
            };
            _mockRepo.Setup(r => r.Get()).ReturnsAsync(lists);

            var request = TestHttpFactory.CreateGetRequest();
            await _controller.RunAll(request);

            _mockRepo.Verify(r => r.Update(It.IsAny<ShoppingList>()), Times.Never);
        }

        // ─── POST ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task RunAll_POST_ReturnsOk_WhenValidListProvided()
        {
            var model = new ShoppingListModel { Id = "new-1", Name = "Ny liste", ShoppingItems = new List<ShoppingListItemModel>() };
            var inserted = new ShoppingList { Id = "new-1", Name = "Ny liste", LastModified = DateTime.UtcNow, ShoppingItems = new List<ShoppingListItem>() };

            _mockRepo.Setup(r => r.Insert(It.IsAny<ShoppingList>())).ReturnsAsync(inserted);

            var json = JsonSerializer.Serialize(model);
            var request = TestHttpFactory.CreatePostRequest(json);
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Ny liste", body);
        }

        [Fact]
        public async Task RunAll_POST_SetsLastModified_WhenCreatingList()
        {
            var model = new ShoppingListModel { Id = "new-2", Name = "Test liste", ShoppingItems = new List<ShoppingListItemModel>() };
            var inserted = new ShoppingList { Id = "new-2", Name = "Test liste", LastModified = DateTime.UtcNow, ShoppingItems = new List<ShoppingListItem>() };

            _mockRepo.Setup(r => r.Insert(It.IsAny<ShoppingList>())).ReturnsAsync(inserted);

            var request = TestHttpFactory.CreatePostRequest(JsonSerializer.Serialize(model));
            await _controller.RunAll(request);

            // Controller should set LastModified before calling Insert
            _mockRepo.Verify(r => r.Insert(It.Is<ShoppingList>(sl => sl.LastModified.HasValue)), Times.Once);
        }

        [Fact]
        public async Task RunAll_POST_ReturnsInternalServerError_WhenRepositoryInsertFails()
        {
            var model = new ShoppingListModel { Id = "fail-1", Name = "Fail liste", ShoppingItems = new List<ShoppingListItemModel>() };
            _mockRepo.Setup(r => r.Insert(It.IsAny<ShoppingList>())).ReturnsAsync((ShoppingList?)null!);

            var request = TestHttpFactory.CreatePostRequest(JsonSerializer.Serialize(model));
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ─── PUT ────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task RunAll_PUT_ReturnsOk_WhenValidListProvided()
        {
            var model = new ShoppingListModel { Id = "1", Name = "Oppdatert liste", ShoppingItems = new List<ShoppingListItemModel>() };
            var updated = new ShoppingList { Id = "1", Name = "Oppdatert liste", LastModified = DateTime.UtcNow, ShoppingItems = new List<ShoppingListItem>() };

            _mockRepo.Setup(r => r.Update(It.IsAny<ShoppingList>())).ReturnsAsync(updated);

            var request = TestHttpFactory.CreatePutRequest(JsonSerializer.Serialize(model));
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Oppdatert liste", body);
        }

        [Fact]
        public async Task RunAll_PUT_SetsLastModified_WhenUpdatingList()
        {
            var model = new ShoppingListModel { Id = "1", Name = "Updated", ShoppingItems = new List<ShoppingListItemModel>() };
            var updated = new ShoppingList { Id = "1", Name = "Updated", LastModified = DateTime.UtcNow, ShoppingItems = new List<ShoppingListItem>() };
            _mockRepo.Setup(r => r.Update(It.IsAny<ShoppingList>())).ReturnsAsync(updated);

            var request = TestHttpFactory.CreatePutRequest(JsonSerializer.Serialize(model));
            await _controller.RunAll(request);

            _mockRepo.Verify(r => r.Update(It.Is<ShoppingList>(sl => sl.LastModified.HasValue)), Times.Once);
        }

        [Fact]
        public async Task RunAll_PUT_ReturnsInternalServerError_WhenRepositoryUpdateFails()
        {
            var model = new ShoppingListModel { Id = "1", Name = "Fail", ShoppingItems = new List<ShoppingListItemModel>() };
            _mockRepo.Setup(r => r.Update(It.IsAny<ShoppingList>())).ReturnsAsync((ShoppingList?)null!);

            var request = TestHttpFactory.CreatePutRequest(JsonSerializer.Serialize(model));
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ─── RunOne GET ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task RunOne_GET_ReturnsOk_WhenListExists()
        {
            var list = new ShoppingList { Id = "1", Name = "Ukeshandel", LastModified = DateTime.UtcNow, ShoppingItems = new List<ShoppingListItem>() };
            _mockRepo.Setup(r => r.Get("1")).ReturnsAsync(list);
            _mockRepo.Setup(r => r.Update(It.IsAny<ShoppingList>())).ReturnsAsync((ShoppingList sl) => sl);

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/shoppinglist/1");
            var response = await _controller.RunOne(request, "1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Ukeshandel", body);
        }

        [Fact]
        public async Task RunOne_GET_ReturnsInternalServerError_WhenListNotFound()
        {
            _mockRepo.Setup(r => r.Get("999")).ReturnsAsync((ShoppingList?)null!);

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/shoppinglist/999");
            var response = await _controller.RunOne(request, "999");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task RunOne_GET_MigratesLastModified_WhenListIsLegacy()
        {
            var legacy = new ShoppingList { Id = "leg-1", Name = "Old", LastModified = null, ShoppingItems = new List<ShoppingListItem>() };
            var migrated = new ShoppingList { Id = "leg-1", Name = "Old", LastModified = DateTime.UtcNow, ShoppingItems = new List<ShoppingListItem>() };
            _mockRepo.Setup(r => r.Get("leg-1")).ReturnsAsync(legacy);
            _mockRepo.Setup(r => r.Update(It.IsAny<ShoppingList>())).ReturnsAsync(migrated);

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/shoppinglist/leg-1");
            var response = await _controller.RunOne(request, "leg-1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _mockRepo.Verify(r => r.Update(It.Is<ShoppingList>(sl => sl.Id == "leg-1" && sl.LastModified.HasValue)), Times.Once);
        }

        // ─── RunOne DELETE ───────────────────────────────────────────────────────────

        [Fact]
        public async Task RunOne_DELETE_ReturnsNoContent_WhenDeleteSucceeds()
        {
            _mockRepo.Setup(r => r.Delete("1")).ReturnsAsync(true);

            var request = TestHttpFactory.CreateDeleteRequest("http://localhost/api/shoppinglist/1");
            var response = await _controller.RunOne(request, "1");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task RunOne_DELETE_ReturnsInternalServerError_WhenDeleteFails()
        {
            _mockRepo.Setup(r => r.Delete("999")).ReturnsAsync(false);

            var request = TestHttpFactory.CreateDeleteRequest("http://localhost/api/shoppinglist/999");
            var response = await _controller.RunOne(request, "999");

            // Delete fails → controller writes error string but falls through to NotFound
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ─── Exception handling ──────────────────────────────────────────────────────

        [Fact]
        public async Task RunAll_GET_ReturnsInternalServerError_WhenRepositoryThrows()
        {
            _mockRepo.Setup(r => r.Get()).ThrowsAsync(new Exception("Firestore unavailable"));

            var request = TestHttpFactory.CreateGetRequest();
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task RunAll_POST_ReturnsInternalServerError_WhenRepositoryThrows()
        {
            var model = new ShoppingListModel { Id = "x", Name = "Error test", ShoppingItems = new List<ShoppingListItemModel>() };
            _mockRepo.Setup(r => r.Insert(It.IsAny<ShoppingList>())).ThrowsAsync(new Exception("DB error"));

            var request = TestHttpFactory.CreatePostRequest(JsonSerializer.Serialize(model));
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }
}
