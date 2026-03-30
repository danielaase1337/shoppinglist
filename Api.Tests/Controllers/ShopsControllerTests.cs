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
    /// Real controller tests — instantiate actual ShopsController with mocked dependencies
    /// and call controller methods directly. Established as Issue #33 fix.
    /// </summary>
    public class ShopsControllerTests
    {
        private readonly Mock<IGenericRepository<Shop>> _mockRepo;
        private readonly Mock<IGenericRepository<ShoppingList>> _mockShoppingListRepo;
        private readonly IMapper _mapper;
        private readonly ShopsController _controller;

        public ShopsControllerTests()
        {
            _mockRepo = new Mock<IGenericRepository<Shop>>();
            _mockShoppingListRepo = new Mock<IGenericRepository<ShoppingList>>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<Api.ShoppingListProfile>();
            });
            _mapper = config.CreateMapper();
            _controller = new ShopsController(NullLoggerFactory.Instance, _mockRepo.Object, _mockShoppingListRepo.Object, _mapper);
        }

        // --- Run GET ----------------------------------------------------------------

        [Fact]
        public async Task Run_GET_ReturnsOk_WhenShopsExist()
        {
            var shops = new List<Shop>
            {
                new Shop { Id = "1", Name = "Rema 1000", ShelfsInShop = new List<Shelf>() },
                new Shop { Id = "2", Name = "ICA Maxi", ShelfsInShop = new List<Shelf>() }
            };
            _mockRepo.Setup(r => r.Get()).ReturnsAsync(shops);

            var request = TestHttpFactory.CreateGetRequest();
            var response = await _controller.Run(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Rema 1000", body);
            Assert.Contains("ICA Maxi", body);
        }

        [Fact]
        public async Task Run_GET_ReturnsOkWithEmptyArray_WhenNoShopsExist()
        {
            _mockRepo.Setup(r => r.Get()).ReturnsAsync(new List<Shop>());

            var request = TestHttpFactory.CreateGetRequest();
            var response = await _controller.Run(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Equal("[]", body.Trim());
        }

        [Fact]
        public async Task Run_GET_Returns500_WhenRepositoryReturnsNull()
        {
            _mockRepo.Setup(r => r.Get()).ReturnsAsync((List<Shop>?)null!);

            var request = TestHttpFactory.CreateGetRequest();
            var response = await _controller.Run(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task Run_GET_Returns500_WhenRepositoryThrows()
        {
            _mockRepo.Setup(r => r.Get()).ThrowsAsync(new Exception("Firestore unavailable"));

            var request = TestHttpFactory.CreateGetRequest();
            var response = await _controller.Run(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // --- Run POST ---------------------------------------------------------------

        [Fact]
        public async Task Run_POST_ReturnsOk_WhenValidShopProvided()
        {
            var shopModel = new ShopModel { Id = "3", Name = "Kiwi", ShelfsInShop = new List<ShelfModel>() };
            var inserted = new Shop { Id = "3", Name = "Kiwi", ShelfsInShop = new List<Shelf>() };
            _mockRepo.Setup(r => r.Insert(It.IsAny<Shop>())).ReturnsAsync(inserted);

            var request = TestHttpFactory.CreatePostRequest(JsonSerializer.Serialize(shopModel));
            var response = await _controller.Run(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Kiwi", body);
        }

        [Fact]
        public async Task Run_POST_Returns500_WhenNullBody()
        {
            var request = TestHttpFactory.CreatePostRequest("null");
            var response = await _controller.Run(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task Run_POST_Returns500_WhenInsertFails()
        {
            var shopModel = new ShopModel { Id = "fail", Name = "Fail Shop", ShelfsInShop = new List<ShelfModel>() };
            _mockRepo.Setup(r => r.Insert(It.IsAny<Shop>())).ReturnsAsync((Shop?)null!);

            var request = TestHttpFactory.CreatePostRequest(JsonSerializer.Serialize(shopModel));
            var response = await _controller.Run(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // --- Run PUT ----------------------------------------------------------------

        [Fact]
        public async Task Run_PUT_ReturnsOk_WhenValidShopProvided()
        {
            var shopModel = new ShopModel { Id = "1", Name = "Rema 1000 Updated", ShelfsInShop = new List<ShelfModel>() };
            var updated = new Shop { Id = "1", Name = "Rema 1000 Updated", ShelfsInShop = new List<Shelf>() };
            _mockRepo.Setup(r => r.Update(It.IsAny<Shop>())).ReturnsAsync(updated);

            var request = TestHttpFactory.CreatePutRequest(JsonSerializer.Serialize(shopModel));
            var response = await _controller.Run(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Rema 1000 Updated", body);
        }

        [Fact]
        public async Task Run_PUT_Returns500_WhenNullBody()
        {
            var request = TestHttpFactory.CreatePutRequest("null");
            var response = await _controller.Run(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task Run_PUT_Returns500_WhenUpdateFails()
        {
            var shopModel = new ShopModel { Id = "1", Name = "Updated", ShelfsInShop = new List<ShelfModel>() };
            _mockRepo.Setup(r => r.Update(It.IsAny<Shop>())).ReturnsAsync((Shop?)null!);

            var request = TestHttpFactory.CreatePutRequest(JsonSerializer.Serialize(shopModel));
            var response = await _controller.Run(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // --- RunOne GET -------------------------------------------------------------

        [Fact]
        public async Task RunOne_GET_ReturnsOk_WhenShopExists()
        {
            var shop = new Shop { Id = "1", Name = "Rema 1000", ShelfsInShop = new List<Shelf>() };
            _mockRepo.Setup(r => r.Get("1")).ReturnsAsync(shop);

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/shop/1");
            var response = await _controller.RunOne(request, "1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Rema 1000", body);
        }

        [Fact]
        public async Task RunOne_GET_Returns500_WhenShopNotFound()
        {
            _mockRepo.Setup(r => r.Get("999")).ReturnsAsync((Shop?)null!);

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/shop/999");
            var response = await _controller.RunOne(request, "999");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // --- RunOne DELETE ----------------------------------------------------------

        [Fact]
        public async Task RunOne_DELETE_ReturnsNoContent_WhenDeleteSucceeds()
        {
            _mockRepo.Setup(r => r.Delete("1")).ReturnsAsync(true);

            var request = TestHttpFactory.CreateDeleteRequest("http://localhost/api/shop/1");
            var response = await _controller.RunOne(request, "1");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task RunOne_DELETE_Returns500_WhenDeleteFails()
        {
            _mockRepo.Setup(r => r.Delete("999")).ReturnsAsync(false);

            var request = TestHttpFactory.CreateDeleteRequest("http://localhost/api/shop/999");
            var response = await _controller.RunOne(request, "999");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // --- Exception handling -----------------------------------------------------

        [Fact]
        public async Task RunOne_GET_Returns500_WhenRepositoryThrows()
        {
            _mockRepo.Setup(r => r.Get(It.IsAny<object>())).ThrowsAsync(new Exception("Timeout"));

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/shop/1");
            var response = await _controller.RunOne(request, "1");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }
}
