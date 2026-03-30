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
    /// Real controller tests — instantiate actual ShopsItemsController with mocked dependencies
    /// and call controller methods directly. Established as Issue #33 fix.
    /// </summary>
    public class ShopsItemsControllerTests
    {
        private readonly Mock<IGenericRepository<ShopItem>> _mockRepo;
        private readonly IMapper _mapper;
        private readonly ShopsItemsController _controller;

        public ShopsItemsControllerTests()
        {
            _mockRepo = new Mock<IGenericRepository<ShopItem>>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<Api.ShoppingListProfile>();
            });
            _mapper = config.CreateMapper();

            _controller = new ShopsItemsController(NullLoggerFactory.Instance, _mockRepo.Object, _mapper);
        }

        // ─── RunAll GET ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task RunAll_GET_ReturnsOk_WhenItemsExist()
        {
            var items = new List<ShopItem>
            {
                new ShopItem { Id = "1", Name = "Melk", Unit = "Liter", ItemCategory = new ItemCategory { Id = "dairy", Name = "Meieri" } },
                new ShopItem { Id = "2", Name = "Brod", Unit = "Stk", ItemCategory = new ItemCategory { Id = "bakery", Name = "Bakeri" } }
            };
            _mockRepo.Setup(r => r.Get()).ReturnsAsync(items);

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/shopitems");
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Melk", body);
            Assert.Contains("Brod", body);
        }

        [Fact]
        public async Task RunAll_GET_Returns500_WhenRepositoryReturnsNull()
        {
            _mockRepo.Setup(r => r.Get()).ReturnsAsync((List<ShopItem>?)null!);

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/shopitems");
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task RunAll_GET_Returns500_WhenRepositoryThrows()
        {
            _mockRepo.Setup(r => r.Get()).ThrowsAsync(new Exception("Firestore unavailable"));

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/shopitems");
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ─── RunAll POST ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task RunAll_POST_ReturnsOk_WhenValidItemProvided()
        {
            var model = new ShopItemModel { Id = "3", Name = "Epler", Unit = "Kg", ItemCategory = new ItemCategoryModel { Id = "fruit", Name = "Frukt" } };
            var inserted = new ShopItem { Id = "3", Name = "Epler", Unit = "Kg", ItemCategory = new ItemCategory { Id = "fruit", Name = "Frukt" } };
            _mockRepo.Setup(r => r.Insert(It.IsAny<ShopItem>())).ReturnsAsync(inserted);

            var request = TestHttpFactory.CreatePostRequest(JsonSerializer.Serialize(model), "http://localhost/api/shopitems");
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Epler", body);
        }

        [Fact]
        public async Task RunAll_POST_ReturnsBadRequest_WhenNullBody()
        {
            var request = TestHttpFactory.CreatePostRequest("null", "http://localhost/api/shopitems");
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RunAll_POST_Returns500_WhenInsertFails()
        {
            var model = new ShopItemModel { Id = "fail", Name = "Fail Item", Unit = "Stk", ItemCategory = new ItemCategoryModel { Id = "x", Name = "X" } };
            _mockRepo.Setup(r => r.Insert(It.IsAny<ShopItem>())).ReturnsAsync((ShopItem?)null!);

            var request = TestHttpFactory.CreatePostRequest(JsonSerializer.Serialize(model), "http://localhost/api/shopitems");
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ─── RunAll PUT ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task RunAll_PUT_ReturnsOk_WhenValidItemProvided()
        {
            var model = new ShopItemModel { Id = "1", Name = "Melk Oppdatert", Unit = "Liter", ItemCategory = new ItemCategoryModel { Id = "dairy", Name = "Meieri" } };
            var updated = new ShopItem { Id = "1", Name = "Melk Oppdatert", Unit = "Liter", ItemCategory = new ItemCategory { Id = "dairy", Name = "Meieri" } };
            _mockRepo.Setup(r => r.Update(It.IsAny<ShopItem>())).ReturnsAsync(updated);

            var request = TestHttpFactory.CreatePutRequest(JsonSerializer.Serialize(model), "http://localhost/api/shopitems");
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Melk Oppdatert", body);
        }

        [Fact]
        public async Task RunAll_PUT_ReturnsBadRequest_WhenNullBody()
        {
            var request = TestHttpFactory.CreatePutRequest("null", "http://localhost/api/shopitems");
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RunAll_PUT_Returns500_WhenUpdateFails()
        {
            var model = new ShopItemModel { Id = "1", Name = "Updated", Unit = "Stk", ItemCategory = new ItemCategoryModel { Id = "x", Name = "X" } };
            _mockRepo.Setup(r => r.Update(It.IsAny<ShopItem>())).ReturnsAsync((ShopItem?)null!);

            var request = TestHttpFactory.CreatePutRequest(JsonSerializer.Serialize(model), "http://localhost/api/shopitems");
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ─── Run GET by id ───────────────────────────────────────────────────────────

        [Fact]
        public async Task Run_GET_ReturnsOk_WhenItemExists()
        {
            var item = new ShopItem { Id = "1", Name = "Melk", Unit = "Liter", ItemCategory = new ItemCategory { Id = "dairy", Name = "Meieri" } };
            _mockRepo.Setup(r => r.Get("1")).ReturnsAsync(item);

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/shopitem/1");
            var response = await _controller.Run(request, "1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Melk", body);
        }

        [Fact]
        public async Task Run_GET_Returns500_WhenItemNotFound()
        {
            _mockRepo.Setup(r => r.Get("999")).ReturnsAsync((ShopItem?)null!);

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/shopitem/999");
            var response = await _controller.Run(request, "999");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ─── Run DELETE ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task Run_DELETE_ReturnsNoContent_WhenDeleteSucceeds()
        {
            _mockRepo.Setup(r => r.Delete("1")).ReturnsAsync(true);

            var request = TestHttpFactory.CreateDeleteRequest("http://localhost/api/shopitem/1");
            var response = await _controller.Run(request, "1");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Run_DELETE_Returns500_WhenDeleteFails()
        {
            _mockRepo.Setup(r => r.Delete("999")).ReturnsAsync(false);

            var request = TestHttpFactory.CreateDeleteRequest("http://localhost/api/shopitem/999");
            var response = await _controller.Run(request, "999");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task Run_Returns400_WhenMethodNotAllowed()
        {
            var request = TestHttpFactory.CreatePutRequest("{}", "http://localhost/api/shopitem/1");
            var response = await _controller.Run(request, "1");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
