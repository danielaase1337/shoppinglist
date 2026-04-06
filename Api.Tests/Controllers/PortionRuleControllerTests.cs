using Api.Controllers;
using Api.Tests.Helpers;
using AutoMapper;
using Shared;
using Microsoft.Azure.Functions.Worker.Http;
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
    /// Unit tests for PortionRuleController.
    /// All tests call actual controller methods (RunAll / RunOne) — not the mock directly.
    ///
    /// Key behaviours tested:
    /// - GET /portionrules: filters to IsActive=true only; inactive rules are hidden
    /// - POST: sets IsActive=true and LastModified regardless of what client sends
    /// - DELETE: soft-delete pattern — Get → IsActive=false → Update. repository.Delete() NEVER called.
    /// </summary>
    public class PortionRuleControllerTests
    {
        private readonly Mock<IGenericRepository<PortionRule>> _mockRepository;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMapper _mapper;
        private readonly PortionRuleController _controller;

        private static readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public PortionRuleControllerTests()
        {
            _mockRepository = new Mock<IGenericRepository<PortionRule>>();
            _loggerFactory = NullLoggerFactory.Instance;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<PortionRule, PortionRuleModel>().ReverseMap();
            });
            _mapper = config.CreateMapper();

            _controller = new PortionRuleController(_loggerFactory, _mockRepository.Object, _mapper);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static async Task<T?> ReadBody<T>(HttpResponseData response)
        {
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            return JsonSerializer.Deserialize<T>(body, _jsonOptions);
        }

        private static List<PortionRule> CreateSampleRules() => new List<PortionRule>
        {
            new PortionRule
            {
                Id = "rule-1",
                Name = "Pasta - Adult",
                ShopItemId = "shopitem-pasta",
                AgeGroup = AgeGroup.Adult,
                QuantityPerPerson = 100,
                Unit = MealUnit.Gram,
                IsActive = true,
                LastModified = DateTime.UtcNow.AddDays(-5)
            },
            new PortionRule
            {
                Id = "rule-2",
                Name = "Pasta - Child",
                ShopItemId = "shopitem-pasta",
                AgeGroup = AgeGroup.Child,
                QuantityPerPerson = 60,
                Unit = MealUnit.Gram,
                IsActive = true,
                LastModified = DateTime.UtcNow.AddDays(-5)
            },
            new PortionRule
            {
                Id = "rule-3",
                Name = "Kjøtt - Adult (inactive)",
                ShopItemId = "shopitem-kjott",
                AgeGroup = AgeGroup.Adult,
                QuantityPerPerson = 150,
                Unit = MealUnit.Gram,
                IsActive = false,
                LastModified = DateTime.UtcNow.AddDays(-30)
            }
        };

        // ── Test 1: GetAll returns only active rules ──────────────────────────────

        [Fact]
        public async Task GetAll_ReturnsActiveRules()
        {
            // Arrange: 2 active + 1 inactive rule
            var rules = CreateSampleRules();
            _mockRepository.Setup(r => r.Get())
                .Returns(Task.FromResult<ICollection<PortionRule>>(rules));

            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/portionrules");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<PortionRuleModel[]>(response);

            // Assert: only the 2 active rules are returned
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(2, result.Length);
            Assert.All(result, r => Assert.True(r.IsActive));
            Assert.DoesNotContain(result, r => r.Name == "Kjøtt - Adult (inactive)");
        }

        // ── Test 2: GetAll returns empty list when none ───────────────────────────

        [Fact]
        public async Task GetAll_ReturnsEmptyList_WhenNone()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get())
                .Returns(Task.FromResult<ICollection<PortionRule>>(new List<PortionRule>()));

            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/portionrules");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<PortionRuleModel[]>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // ── Test 3: GetAll returns 500 when repository returns null ───────────────

        [Fact]
        public async Task GetAll_ReturnsError_WhenNull()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get())
                .Returns(Task.FromResult<ICollection<PortionRule>>(null!));

            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/portionrules");

            // Act
            var response = await _controller.RunAll(req);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ── Test 4: POST sets IsActive=true and LastModified ──────────────────────

        [Fact]
        public async Task Create_SetsIsActiveAndLastModified()
        {
            // Arrange: client sends IsActive=false — controller must override to true
            var newRuleModel = new PortionRuleModel
            {
                Id = "rule-new",
                Name = "Ris - Toddler",
                ShopItemId = "shopitem-ris",
                AgeGroup = AgeGroup.Toddler,
                QuantityPerPerson = 40,
                Unit = MealUnit.Gram,
                IsActive = false  // controller should force this to true
            };

            var saved = _mapper.Map<PortionRule>(newRuleModel);
            saved.IsActive = true;
            saved.LastModified = DateTime.UtcNow;

            _mockRepository
                .Setup(r => r.Insert(It.IsAny<PortionRule>()))
                .ReturnsAsync(saved);

            var body = JsonSerializer.Serialize(newRuleModel);
            var req = TestHttpFactory.CreatePostRequest(body, "http://localhost/api/portionrules");

            // Act
            await _controller.RunAll(req);

            // Assert: controller forced IsActive=true and set LastModified
            _mockRepository.Verify(r => r.Insert(It.Is<PortionRule>(
                rule => rule.IsActive == true && rule.LastModified.HasValue
            )), Times.Once);
        }

        // ── Test 5: POST returns created rule ─────────────────────────────────────

        [Fact]
        public async Task Create_ReturnsCreatedRule()
        {
            // Arrange
            var newRuleModel = new PortionRuleModel
            {
                Id = "rule-new",
                Name = "Ris - Adult",
                ShopItemId = "shopitem-ris",
                AgeGroup = AgeGroup.Adult,
                QuantityPerPerson = 80,
                Unit = MealUnit.Gram,
                IsActive = true
            };

            var saved = _mapper.Map<PortionRule>(newRuleModel);
            saved.LastModified = DateTime.UtcNow;

            _mockRepository
                .Setup(r => r.Insert(It.IsAny<PortionRule>()))
                .ReturnsAsync(saved);

            var body = JsonSerializer.Serialize(newRuleModel);
            var req = TestHttpFactory.CreatePostRequest(body, "http://localhost/api/portionrules");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<PortionRuleModel>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal("Ris - Adult", result.Name);
            Assert.Equal(80, result.QuantityPerPerson);
            Assert.True(result.IsActive);
            Assert.NotNull(result.LastModified);
        }

        // ── Test 6: PUT sets LastModified before updating ─────────────────────────

        [Fact]
        public async Task Update_SetsLastModified()
        {
            // Arrange
            var oldTimestamp = DateTime.UtcNow.AddDays(-7);
            var existingModel = new PortionRuleModel
            {
                Id = "rule-1",
                Name = "Pasta - Adult (oppdatert)",
                ShopItemId = "shopitem-pasta",
                AgeGroup = AgeGroup.Adult,
                QuantityPerPerson = 120,
                Unit = MealUnit.Gram,
                IsActive = true,
                LastModified = oldTimestamp
            };

            var updated = _mapper.Map<PortionRule>(existingModel);
            updated.LastModified = DateTime.UtcNow;

            _mockRepository
                .Setup(r => r.Update(It.IsAny<PortionRule>()))
                .ReturnsAsync(updated);

            var body = JsonSerializer.Serialize(existingModel);
            var req = TestHttpFactory.CreatePutRequest(body, "http://localhost/api/portionrules");

            // Act
            var response = await _controller.RunAll(req);

            // Assert: controller refreshed LastModified (newer than old)
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _mockRepository.Verify(r => r.Update(It.Is<PortionRule>(
                rule => rule.LastModified.HasValue && rule.LastModified.Value > oldTimestamp
            )), Times.Once);
        }

        // ── Test 7: GET by id returns rule when found ─────────────────────────────

        [Fact]
        public async Task GetById_ReturnsRule_WhenFound()
        {
            // Arrange
            var rule = new PortionRule
            {
                Id = "rule-1",
                Name = "Pasta - Adult",
                ShopItemId = "shopitem-pasta",
                AgeGroup = AgeGroup.Adult,
                QuantityPerPerson = 100,
                Unit = MealUnit.Gram,
                IsActive = true,
                LastModified = DateTime.UtcNow
            };
            _mockRepository.Setup(r => r.Get("rule-1")).ReturnsAsync(rule);

            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/portionrule/rule-1");

            // Act
            var response = await _controller.RunOne(req, "rule-1");
            var result = await ReadBody<PortionRuleModel>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal("Pasta - Adult", result.Name);
            Assert.Equal("rule-1", result.Id);
            Assert.Equal(AgeGroup.Adult, result.AgeGroup);
            Assert.Equal(100, result.QuantityPerPerson);
        }

        // ── Test 8: GET by id returns 404 when not found ──────────────────────────

        [Fact]
        public async Task GetById_Returns404_WhenNotFound()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get("nonexistent")).ReturnsAsync((PortionRule?)null);
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/portionrule/nonexistent");

            // Act
            var response = await _controller.RunOne(req, "nonexistent");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ── Test 9: DELETE soft-deletes by setting IsActive=false ─────────────────

        [Fact]
        public async Task Delete_SoftDeletes_SetsIsActiveFalse()
        {
            // Arrange
            var existingRule = new PortionRule
            {
                Id = "rule-1",
                Name = "Pasta - Adult",
                ShopItemId = "shopitem-pasta",
                AgeGroup = AgeGroup.Adult,
                QuantityPerPerson = 100,
                Unit = MealUnit.Gram,
                IsActive = true
            };

            var softDeletedRule = new PortionRule
            {
                Id = "rule-1",
                Name = "Pasta - Adult",
                ShopItemId = "shopitem-pasta",
                AgeGroup = AgeGroup.Adult,
                QuantityPerPerson = 100,
                Unit = MealUnit.Gram,
                IsActive = false,
                LastModified = DateTime.UtcNow
            };

            _mockRepository.Setup(r => r.Get("rule-1")).ReturnsAsync(existingRule);
            _mockRepository
                .Setup(r => r.Update(It.Is<PortionRule>(rule => !rule.IsActive)))
                .ReturnsAsync(softDeletedRule);

            var req = TestHttpFactory.CreateDeleteRequest("http://localhost/api/portionrule/rule-1");

            // Act
            var response = await _controller.RunOne(req, "rule-1");
            var result = await ReadBody<PortionRuleModel>(response);

            // Assert: response returns the rule with IsActive=false
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.False(result.IsActive);

            // Verify soft-delete: Update called with IsActive=false
            _mockRepository.Verify(r => r.Update(It.Is<PortionRule>(rule => !rule.IsActive)), Times.Once);
        }

        // ── Test 10: DELETE never calls repository.Delete ─────────────────────────

        [Fact]
        public async Task Delete_DoesNotCallRepositoryDelete()
        {
            // Arrange
            var existingRule = new PortionRule
            {
                Id = "rule-1",
                Name = "Pasta - Adult",
                ShopItemId = "shopitem-pasta",
                AgeGroup = AgeGroup.Adult,
                QuantityPerPerson = 100,
                Unit = MealUnit.Gram,
                IsActive = true
            };

            var softDeletedRule = new PortionRule
            {
                Id = "rule-1",
                Name = "Pasta - Adult",
                ShopItemId = "shopitem-pasta",
                AgeGroup = AgeGroup.Adult,
                QuantityPerPerson = 100,
                Unit = MealUnit.Gram,
                IsActive = false,
                LastModified = DateTime.UtcNow
            };

            _mockRepository.Setup(r => r.Get("rule-1")).ReturnsAsync(existingRule);
            _mockRepository
                .Setup(r => r.Update(It.IsAny<PortionRule>()))
                .ReturnsAsync(softDeletedRule);

            var req = TestHttpFactory.CreateDeleteRequest("http://localhost/api/portionrule/rule-1");

            // Act
            await _controller.RunOne(req, "rule-1");

            // Assert: hard delete was never triggered
            _mockRepository.Verify(r => r.Delete(It.IsAny<object>()), Times.Never);
            _mockRepository.Verify(r => r.Delete(It.IsAny<PortionRule>()), Times.Never);
        }
    }
}
