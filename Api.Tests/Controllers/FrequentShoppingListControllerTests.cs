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
    /// Real controller tests — instantiate actual FrequentShoppingListController with mocked dependencies
    /// and call controller methods directly. Established as Issue #33 fix.
    /// </summary>
    public class FrequentShoppingListControllerTests
    {
        private readonly Mock<IGenericRepository<FrequentShoppingList>> _mockRepo;
        private readonly IMapper _mapper;
        private readonly FrequentShoppingListController _controller;

        public FrequentShoppingListControllerTests()
        {
            _mockRepo = new Mock<IGenericRepository<FrequentShoppingList>>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<Api.ShoppingListProfile>();
            });
            _mapper = config.CreateMapper();

            _controller = new FrequentShoppingListController(NullLoggerFactory.Instance, _mockRepo.Object, _mapper);
        }

        // ─── RunAll GET ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task RunAll_GET_ReturnsOk_WhenListsExist()
        {
            var lists = new List<FrequentShoppingList>
            {
                new FrequentShoppingList { Id = "1", Name = "Uke 1", Items = new List<FrequentShoppingItem>() },
                new FrequentShoppingList { Id = "2", Name = "Uke 2", Items = new List<FrequentShoppingItem>() }
            };
            _mockRepo.Setup(r => r.Get()).ReturnsAsync(lists);

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/frequentshoppinglists");
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Uke 1", body);
            Assert.Contains("Uke 2", body);
        }

        [Fact]
        public async Task RunAll_GET_ReturnsOkWithEmptyArray_WhenNoListsExist()
        {
            _mockRepo.Setup(r => r.Get()).ReturnsAsync(new List<FrequentShoppingList>());

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/frequentshoppinglists");
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Equal("[]", body.Trim());
        }

        [Fact]
        public async Task RunAll_GET_Returns500_WhenRepositoryThrows()
        {
            _mockRepo.Setup(r => r.Get()).ThrowsAsync(new Exception("Firestore unavailable"));

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/frequentshoppinglists");
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ─── RunAll POST ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task RunAll_POST_Returns201_WhenValidListProvided()
        {
            var model = new FrequentShoppingListModel { Id = "new-1", Name = "Ny hyppig liste", Items = new List<FrequentShoppingItemModel>() };
            var inserted = new FrequentShoppingList { Id = "new-1", Name = "Ny hyppig liste", Items = new List<FrequentShoppingItem>() };
            _mockRepo.Setup(r => r.Insert(It.IsAny<FrequentShoppingList>())).ReturnsAsync(inserted);

            var request = TestHttpFactory.CreatePostRequest(JsonSerializer.Serialize(model), "http://localhost/api/frequentshoppinglists");
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Ny hyppig liste", body);
        }

        [Fact]
        public async Task RunAll_POST_Returns400_WhenNullBody()
        {
            var request = TestHttpFactory.CreatePostRequest("null", "http://localhost/api/frequentshoppinglists");
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RunAll_POST_Returns500_WhenRepositoryThrows()
        {
            var model = new FrequentShoppingListModel { Id = "x", Name = "ErrorList", Items = new List<FrequentShoppingItemModel>() };
            _mockRepo.Setup(r => r.Insert(It.IsAny<FrequentShoppingList>())).ThrowsAsync(new Exception("DB error"));

            var request = TestHttpFactory.CreatePostRequest(JsonSerializer.Serialize(model), "http://localhost/api/frequentshoppinglists");
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ─── RunAll PUT ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task RunAll_PUT_ReturnsOk_WhenValidListProvided()
        {
            var model = new FrequentShoppingListModel { Id = "1", Name = "Oppdatert liste", Items = new List<FrequentShoppingItemModel>() };
            var updated = new FrequentShoppingList { Id = "1", Name = "Oppdatert liste", Items = new List<FrequentShoppingItem>() };
            _mockRepo.Setup(r => r.Update(It.IsAny<FrequentShoppingList>())).ReturnsAsync(updated);

            var request = TestHttpFactory.CreatePutRequest(JsonSerializer.Serialize(model), "http://localhost/api/frequentshoppinglists");
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Oppdatert liste", body);
        }

        [Fact]
        public async Task RunAll_PUT_Returns400_WhenNullBody()
        {
            var request = TestHttpFactory.CreatePutRequest("null", "http://localhost/api/frequentshoppinglists");
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RunAll_PUT_Returns400_WhenIdIsEmpty()
        {
            var model = new FrequentShoppingListModel { Id = "", Name = "No ID List", Items = new List<FrequentShoppingItemModel>() };

            var request = TestHttpFactory.CreatePutRequest(JsonSerializer.Serialize(model), "http://localhost/api/frequentshoppinglists");
            var response = await _controller.RunAll(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ─── RunOne GET ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task RunOne_GET_ReturnsOk_WhenListExists()
        {
            var list = new FrequentShoppingList { Id = "1", Name = "Uke 1", Items = new List<FrequentShoppingItem>() };
            _mockRepo.Setup(r => r.Get("1")).ReturnsAsync(list);

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/frequentshoppinglist/1");
            var response = await _controller.RunOne(request, "1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            Assert.Contains("Uke 1", body);
        }

        [Fact]
        public async Task RunOne_GET_Returns404_WhenListNotFound()
        {
            _mockRepo.Setup(r => r.Get("999")).ReturnsAsync((FrequentShoppingList?)null!);

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/frequentshoppinglist/999");
            var response = await _controller.RunOne(request, "999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ─── RunOne DELETE ───────────────────────────────────────────────────────────

        [Fact]
        public async Task RunOne_DELETE_ReturnsNoContent_WhenDeleteSucceeds()
        {
            _mockRepo.Setup(r => r.Delete("1")).ReturnsAsync(true);

            var request = TestHttpFactory.CreateDeleteRequest("http://localhost/api/frequentshoppinglist/1");
            var response = await _controller.RunOne(request, "1");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task RunOne_DELETE_Returns404_WhenListNotFound()
        {
            _mockRepo.Setup(r => r.Delete("999")).ReturnsAsync(false);

            var request = TestHttpFactory.CreateDeleteRequest("http://localhost/api/frequentshoppinglist/999");
            var response = await _controller.RunOne(request, "999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RunOne_GET_Returns500_WhenRepositoryThrows()
        {
            _mockRepo.Setup(r => r.Get(It.IsAny<string>())).ThrowsAsync(new Exception("DB failure"));

            var request = TestHttpFactory.CreateGetRequest("http://localhost/api/frequentshoppinglist/1");
            var response = await _controller.RunOne(request, "1");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }
}
