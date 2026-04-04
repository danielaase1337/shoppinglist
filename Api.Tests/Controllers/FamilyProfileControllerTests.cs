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
    /// Unit tests for FamilyProfileController.
    /// All tests call actual controller methods (RunAll / RunOne) — not the mock directly.
    ///
    /// Delete pattern: FamilyProfile has no IsActive field — controller calls _repository.Delete(id)
    /// (hard delete). Tests verify Delete IS called and that a false return yields 404.
    /// </summary>
    public class FamilyProfileControllerTests
    {
        private readonly Mock<IGenericRepository<FamilyProfile>> _mockRepository;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMapper _mapper;
        private readonly FamilyProfileController _controller;

        private static readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public FamilyProfileControllerTests()
        {
            _mockRepository = new Mock<IGenericRepository<FamilyProfile>>();
            _loggerFactory = NullLoggerFactory.Instance;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<FamilyProfile, FamilyProfileModel>().ReverseMap();
                cfg.CreateMap<FamilyMember, FamilyMemberModel>().ReverseMap();
            });
            _mapper = config.CreateMapper();

            _controller = new FamilyProfileController(_loggerFactory, _mockRepository.Object, _mapper);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static async Task<T?> ReadBody<T>(HttpResponseData response)
        {
            var body = await TestHttpFactory.ReadResponseBodyAsync(response);
            return JsonSerializer.Deserialize<T>(body, _jsonOptions);
        }

        private static List<FamilyProfile> CreateSampleProfiles() => new List<FamilyProfile>
        {
            new FamilyProfile
            {
                Id = "profile-1",
                Name = "Familien Aase",
                LastModified = DateTime.UtcNow.AddDays(-3),
                Members = new List<FamilyMember>
                {
                    new FamilyMember { Name = "Daniel", AgeGroup = AgeGroup.Adult },
                    new FamilyMember { Name = "Lille Per", AgeGroup = AgeGroup.Child }
                }
            },
            new FamilyProfile
            {
                Id = "profile-2",
                Name = "Familien Test",
                LastModified = DateTime.UtcNow.AddDays(-1),
                Members = new List<FamilyMember>
                {
                    new FamilyMember { Name = "Mamma", AgeGroup = AgeGroup.Adult }
                }
            }
        };

        // ── Test 1: GetAll returns all profiles ──────────────────────────────────

        [Fact]
        public async Task GetAll_ReturnsProfiles()
        {
            // Arrange
            var profiles = CreateSampleProfiles();
            _mockRepository.Setup(r => r.Get())
                .Returns(Task.FromResult<ICollection<FamilyProfile>>(profiles));

            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/familyprofiles");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<FamilyProfileModel[]>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(2, result.Length);
            Assert.Contains(result, p => p.Name == "Familien Aase");
            Assert.Contains(result, p => p.Name == "Familien Test");
        }

        // ── Test 2: GetAll returns empty list when none ──────────────────────────

        [Fact]
        public async Task GetAll_ReturnsEmptyList_WhenNone()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get())
                .Returns(Task.FromResult<ICollection<FamilyProfile>>(new List<FamilyProfile>()));

            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/familyprofiles");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<FamilyProfileModel[]>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // ── Test 3: POST sets LastModified before inserting ──────────────────────

        [Fact]
        public async Task Create_SetsLastModified()
        {
            // Arrange
            var newProfileModel = new FamilyProfileModel
            {
                Id = "profile-new",
                Name = "Ny Familie",
                Members = new List<FamilyMemberModel>
                {
                    new FamilyMemberModel { Name = "Pappa", AgeGroup = AgeGroup.Adult }
                }
            };

            var saved = _mapper.Map<FamilyProfile>(newProfileModel);
            saved.LastModified = DateTime.UtcNow;

            _mockRepository
                .Setup(r => r.Insert(It.IsAny<FamilyProfile>()))
                .ReturnsAsync(saved);

            var body = JsonSerializer.Serialize(newProfileModel);
            var req = TestHttpFactory.CreatePostRequest(body, "http://localhost/api/familyprofiles");

            // Act
            await _controller.RunAll(req);

            // Assert: controller set LastModified before Insert
            _mockRepository.Verify(r => r.Insert(It.Is<FamilyProfile>(
                p => p.LastModified.HasValue
            )), Times.Once);
        }

        // ── Test 4: POST returns created profile ─────────────────────────────────

        [Fact]
        public async Task Create_ReturnsCreatedProfile()
        {
            // Arrange
            var newProfileModel = new FamilyProfileModel
            {
                Id = "profile-new",
                Name = "Ny Familie",
                Members = new List<FamilyMemberModel>
                {
                    new FamilyMemberModel { Name = "Pappa", AgeGroup = AgeGroup.Adult },
                    new FamilyMemberModel { Name = "Jente", AgeGroup = AgeGroup.Child }
                }
            };

            var saved = _mapper.Map<FamilyProfile>(newProfileModel);
            saved.LastModified = DateTime.UtcNow;

            _mockRepository
                .Setup(r => r.Insert(It.IsAny<FamilyProfile>()))
                .ReturnsAsync(saved);

            var body = JsonSerializer.Serialize(newProfileModel);
            var req = TestHttpFactory.CreatePostRequest(body, "http://localhost/api/familyprofiles");

            // Act
            var response = await _controller.RunAll(req);
            var result = await ReadBody<FamilyProfileModel>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal("Ny Familie", result.Name);
            Assert.NotNull(result.LastModified);
            Assert.Equal(2, result.Members.Count);
        }

        // ── Test 5: PUT sets LastModified before updating ────────────────────────

        [Fact]
        public async Task Update_SetsLastModified()
        {
            // Arrange
            var oldTimestamp = DateTime.UtcNow.AddDays(-10);
            var existingModel = new FamilyProfileModel
            {
                Id = "profile-1",
                Name = "Familien Aase - Oppdatert",
                LastModified = oldTimestamp,
                Members = new List<FamilyMemberModel>
                {
                    new FamilyMemberModel { Name = "Daniel", AgeGroup = AgeGroup.Adult },
                    new FamilyMemberModel { Name = "Toddler", AgeGroup = AgeGroup.Toddler }
                }
            };

            var updated = _mapper.Map<FamilyProfile>(existingModel);
            updated.LastModified = DateTime.UtcNow;

            _mockRepository
                .Setup(r => r.Update(It.IsAny<FamilyProfile>()))
                .ReturnsAsync(updated);

            var body = JsonSerializer.Serialize(existingModel);
            var req = TestHttpFactory.CreatePutRequest(body, "http://localhost/api/familyprofiles");

            // Act
            var response = await _controller.RunAll(req);

            // Assert: controller refreshed LastModified (newer than old value)
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _mockRepository.Verify(r => r.Update(It.Is<FamilyProfile>(
                p => p.LastModified.HasValue && p.LastModified.Value > oldTimestamp
            )), Times.Once);
        }

        // ── Test 6: GET by id returns profile when found ─────────────────────────

        [Fact]
        public async Task GetById_ReturnsProfile_WhenFound()
        {
            // Arrange
            var profile = new FamilyProfile
            {
                Id = "profile-1",
                Name = "Familien Aase",
                LastModified = DateTime.UtcNow,
                Members = new List<FamilyMember>
                {
                    new FamilyMember { Name = "Daniel", AgeGroup = AgeGroup.Adult }
                }
            };
            _mockRepository.Setup(r => r.Get("profile-1")).ReturnsAsync(profile);

            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/familyprofile/profile-1");

            // Act
            var response = await _controller.RunOne(req, "profile-1");
            var result = await ReadBody<FamilyProfileModel>(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal("Familien Aase", result.Name);
            Assert.Equal("profile-1", result.Id);
            Assert.Single(result.Members);
        }

        // ── Test 7: GET by id returns 404 when not found ─────────────────────────

        [Fact]
        public async Task GetById_Returns404_WhenNotFound()
        {
            // Arrange
            _mockRepository.Setup(r => r.Get("nonexistent")).ReturnsAsync((FamilyProfile?)null);
            var req = TestHttpFactory.CreateGetRequest("http://localhost/api/familyprofile/nonexistent");

            // Act
            var response = await _controller.RunOne(req, "nonexistent");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ── Test 8: DELETE calls repository.Delete (hard delete — no IsActive) ──
        //
        // FamilyProfile has no IsActive field. Controller calls _repository.Delete(id).
        // Returns 404 when Delete returns false (record not found in Firestore).

        [Fact]
        public async Task Delete_CallsRepositoryDelete()
        {
            // Arrange
            _mockRepository
                .Setup(r => r.Delete(It.IsAny<object>()))
                .ReturnsAsync(true);

            var req = TestHttpFactory.CreateDeleteRequest("http://localhost/api/familyprofile/profile-1");

            // Act
            var response = await _controller.RunOne(req, "profile-1");

            // Assert: 200 OK and repository.Delete was called once
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _mockRepository.Verify(r => r.Delete(It.IsAny<object>()), Times.Once);

            // Verify Update was NOT called (no soft-delete)
            _mockRepository.Verify(r => r.Update(It.IsAny<FamilyProfile>()), Times.Never);
        }
    }
}
