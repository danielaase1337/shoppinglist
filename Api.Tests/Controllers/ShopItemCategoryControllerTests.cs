using Api.Controllers;
using Api.Tests.Helpers;
using AutoMapper;
using Microsoft.Extensions.Logging;
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
    /// Real controller tests — instantiate actual ShopItemCategoryController with mocked dependencies
    /// and call controller methods directly. Established as Issue #33 fix.
    /// </summary>
    public class ShopItemCategoryControllerTests
    {
        private readonly Mock<IGenericRepository<ItemCategory>> _mockRepo;
        private readonly IMapper _mapper;
        private readonly ShopItemCategoryController _controller;

        public ShopItemCategoryControllerTests()
        {
            _mockRepo = new Mock<IGenericRepository<ItemCategory>>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<Api.ShoppingListProfile>();
            });
            _mapper = config.CreateMapper();

            _controller = new ShopItemCategoryController(NullLoggerFactory.Instance, _mockRepo.Object, _mapper);
        }

        // ─── Run GET ────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Run_GET_ReturnsOk_WhenCategoriesExist()
        {
            var categories = new List<ItemCategory>
            {
                new ItemCategory { Id = "1", Name = "Meieri" },
                new ItemCategory { Id = "2", Name = "Bakeri" }
            };
            _mockRepo.Setup(r => r.Get()).ReturnsAsync(categories);

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/itemcategorys");
            var response = await _controller.Run(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Meieri", body);
            Assert.Contains("Bakeri", body);
        }

        [Fact]
        public async Task Run_GET_Returns500_WhenRepositoryReturnsNull()
        {
            _mockRepo.Setup(r => r.Get()).ReturnsAsync((List<ItemCategory>?)null!);

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/itemcategorys");
            var response = await _controller.Run(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ─── Run POST ───────────────────────────────────────────────────────────────

        [Fact]
        public async Task Run_POST_ReturnsOk_WhenValidCategoryProvided()
        {
            var model = new ItemCategoryModel { Id = "3", Name = "Kjott" };
            var inserted = new ItemCategory { Id = "3", Name = "Kjott" };
            _mockRepo.Setup(r => r.Insert(It.IsAny<ItemCategory>())).ReturnsAsync(inserted);

            var request = TestHttpFactory.CreatePostRequest(JsonSerializer.Serialize(model), "http://localhost/api/itemcategorys");
            var response = await _controller.Run(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Kjott", body);
        }

        [Fact]
        public async Task Run_POST_Returns500_WhenNullBody()
        {
            var request = TestHttpFactory.CreatePostRequest("null", "http://localhost/api/itemcategorys");
            var response = await _controller.Run(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task Run_POST_Returns500_WhenInsertFails()
        {
            var model = new ItemCategoryModel { Id = "fail", Name = "FailCat" };
            _mockRepo.Setup(r => r.Insert(It.IsAny<ItemCategory>())).ReturnsAsync((ItemCategory?)null!);

            var request = TestHttpFactory.CreatePostRequest(JsonSerializer.Serialize(model), "http://localhost/api/itemcategorys");
            var response = await _controller.Run(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ─── Run PUT ────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Run_PUT_ReturnsOk_WhenValidCategoryProvided()
        {
            var model = new ItemCategoryModel { Id = "1", Name = "Meieri Updated" };
            var updated = new ItemCategory { Id = "1", Name = "Meieri Updated" };
            _mockRepo.Setup(r => r.Update(It.IsAny<ItemCategory>())).ReturnsAsync(updated);

            var request = TestHttpFactory.CreatePutRequest(JsonSerializer.Serialize(model), "http://localhost/api/itemcategorys");
            var response = await _controller.Run(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Meieri Updated", body);
        }

        [Fact]
        public async Task Run_PUT_Returns500_WhenNullBody()
        {
            var request = TestHttpFactory.CreatePutRequest("null", "http://localhost/api/itemcategorys");
            var response = await _controller.Run(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task Run_PUT_Returns500_WhenUpdateFails()
        {
            var model = new ItemCategoryModel { Id = "1", Name = "Updated" };
            _mockRepo.Setup(r => r.Update(It.IsAny<ItemCategory>())).ReturnsAsync((ItemCategory?)null!);

            var request = TestHttpFactory.CreatePutRequest(JsonSerializer.Serialize(model), "http://localhost/api/itemcategorys");
            var response = await _controller.Run(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ─── RunOne GET ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task RunOne_GET_ReturnsOk_WhenCategoryExists()
        {
            var category = new ItemCategory { Id = "1", Name = "Meieri" };
            _mockRepo.Setup(r => r.Get("1")).ReturnsAsync(category);

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/itemcategory/1");
            var response = await _controller.RunOne(request, "1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Meieri", body);
        }

        [Fact]
        public async Task RunOne_GET_Returns500_WhenCategoryNotFound()
        {
            _mockRepo.Setup(r => r.Get("999")).ReturnsAsync((ItemCategory?)null!);

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/itemcategory/999");
            var response = await _controller.RunOne(request, "999");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ─── RunOne DELETE ───────────────────────────────────────────────────────────

        [Fact]
        public async Task RunOne_DELETE_ReturnsNoContent_WhenDeleteSucceeds()
        {
            _mockRepo.Setup(r => r.Delete("1")).ReturnsAsync(true);

            var request = TestHttpFactory.CreateDeleteRequest("http://localhost/api/itemcategory/1");
            var response = await _controller.RunOne(request, "1");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task RunOne_DELETE_Returns500_WhenDeleteFails()
        {
            _mockRepo.Setup(r => r.Delete("999")).ReturnsAsync(false);

            var request = TestHttpFactory.CreateDeleteRequest("http://localhost/api/itemcategory/999");
            var response = await _controller.RunOne(request, "999");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ─── Exception handling (security: no internal details leaked) ───────────────

        [Fact]
        public async Task RunOne_GET_Returns500WithGenericMessage_WhenRepositoryThrows()
        {
            var throwingRepo = new Mock<IGenericRepository<ItemCategory>>();
            throwingRepo.Setup(r => r.Get(It.IsAny<object>()))
                .ThrowsAsync(new InvalidOperationException("Firestore connection string exposed in error"));

            var controller = new ShopItemCategoryController(NullLoggerFactory.Instance, throwingRepo.Object, _mapper);

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/itemcategory/test-id");
            var response = await controller.RunOne(request, "test-id");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("An unexpected error occurred", body);
            Assert.DoesNotContain("Firestore connection string exposed", body);
        }
    }
}