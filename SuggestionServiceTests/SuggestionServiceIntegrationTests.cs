using AnonymousDomain.Enums;
using AnonymousDomain.Models.AnonymousUser;
using AnonymousDomain.Models.Suggestion;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SuggestionService.Clients;
using SuggestionService.Models.DTOs;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace SuggestionService.Tests.Integration
{
    // Test stub: always reports the box as active so create flows succeed.
    public class ActiveSuggestionBoxServiceClient : SuggestionBoxServiceClient
    {
        public override Task<bool> IsBoxActiveAsync(Guid boxId, string? bearerHeader, CancellationToken requestCt)
            => Task.FromResult(true);
    }

    public class SuggestionServiceWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<SuggestionContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<SuggestionContext>(options =>
                    options.UseInMemoryDatabase("SuggestionIntegrationTestDb"));

                // Replace the box-status gate with a stub that always allows submissions
                var boxClientDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(SuggestionBoxServiceClient));
                if (boxClientDescriptor != null)
                    services.Remove(boxClientDescriptor);
                services.AddScoped<SuggestionBoxServiceClient, ActiveSuggestionBoxServiceClient>();
            });

            builder.UseEnvironment("Testing");
        }
    }

    public class SuggestionCategoryIntegrationTests : IClassFixture<SuggestionServiceWebAppFactory>
    {
        private readonly HttpClient _client;
        private readonly SuggestionServiceWebAppFactory _factory;

        public SuggestionCategoryIntegrationTests(SuggestionServiceWebAppFactory factory)
        {
            _factory = factory;
            _client  = factory.CreateClient();
        }

        private Guid SeedCategory(string title = "Test Category", string description = "Opis")
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SuggestionContext>();
            var category = new SuggestionCategory
            {
                Id          = Guid.NewGuid(),
                Title       = title,
                Description = description
            };
            context.SuggestionCategories.Add(category);
            context.SaveChanges();
            return category.Id;
        }

        [Fact]
        public async Task GetAllSuggestionCategories_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/SuggestionCategory");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAllSuggestionCategories_ReturnsJsonContentType()
        {
            var response = await _client.GetAsync("/api/SuggestionCategory");

            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task GetSuggestionCategoryById_ReturnsOk_WhenFound()
        {
            var id = SeedCategory("Get By Id Category");

            var response = await _client.GetAsync($"/api/SuggestionCategory/{id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetSuggestionCategoryById_ReturnsCorrectCategory()
        {
            var id = SeedCategory("Specific Category", "Opis kategorije");

            var response = await _client.GetAsync($"/api/SuggestionCategory/{id}");
            var returned = await response.Content.ReadFromJsonAsync<SuggestionCategoryDTO>();

            Assert.NotNull(returned);
            Assert.Equal(id, returned!.Id);
            Assert.Equal("Specific Category", returned.Title);
        }

        [Fact]
        public async Task CreateSuggestionCategory_ReturnsCreated()
        {
            var dto = new SuggestionCategoryDTO
            {
                Id          = Guid.NewGuid(),
                Title       = "Nova kategorija",
                Description = "Opis"
            };

            var response = await _client.PostAsJsonAsync("/api/SuggestionCategory", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateSuggestionCategory_ReturnsCorrectData()
        {
            var dto = new SuggestionCategoryDTO
            {
                Id          = Guid.NewGuid(),
                Title       = "Kreirana kategorija",
                Description = "Opis kategorije"
            };

            var response = await _client.PostAsJsonAsync("/api/SuggestionCategory", dto);
            var returned = await response.Content.ReadFromJsonAsync<SuggestionCategoryDTO>();

            Assert.NotNull(returned);
            Assert.Equal("Kreirana kategorija", returned!.Title);
        }

        [Fact]
        public async Task DeleteSuggestionCategory_ReturnsNoContent()
        {
            var id = SeedCategory("Za brisanje");

            var response = await _client.DeleteAsync($"/api/SuggestionCategory/{id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    public class SuggestionIntegrationTests : IClassFixture<SuggestionServiceWebAppFactory>
    {
        private readonly HttpClient _client;
        private readonly SuggestionServiceWebAppFactory _factory;

        public SuggestionIntegrationTests(SuggestionServiceWebAppFactory factory)
        {
            _factory = factory;
            _client  = factory.CreateClient();
        }

        private Guid SeedAnonymousUser()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SuggestionContext>();
            var link = new BoxAccessLink
            {
                Id          = Guid.NewGuid(),
                AccessToken = "test-token",
                IsActive    = true,
                CreatedAt   = DateTime.UtcNow,
                ExpiresAt   = DateTime.UtcNow.AddDays(7)
            };
            context.Set<BoxAccessLink>().Add(link);
            var user = new AnonymousUser
            {
                Id              = Guid.NewGuid(),
                CreatedAt       = DateTime.UtcNow,
                BoxAccessLinkId = link.Id
            };
            context.SaveChanges();
            return user.Id;
        }

        private Guid SeedSuggestion()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SuggestionContext>();
            var userId = SeedAnonymousUser();
            var suggestion = new Suggestion
            {
                Id              = Guid.NewGuid(),
                Title           = "Test Suggestion",
                Description     = "Opis",
                CreatedAt       = DateTime.UtcNow,
                SuggestionBoxId = Guid.NewGuid(),
                AnonymousUserId = userId,
                Status          = ProblemSuggestionStatus.Active
            };
            context.Suggestions.Add(suggestion);
            context.SaveChanges();
            return suggestion.Id;
        }

        [Fact]
        public async Task GetAllSuggestions_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/Suggestion");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAllSuggestions_ReturnsJsonContentType()
        {
            var response = await _client.GetAsync("/api/Suggestion");

            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task GetSuggestionById_ReturnsOk_WhenFound()
        {
            var id = SeedSuggestion();

            var response = await _client.GetAsync($"/api/Suggestion/{id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetSuggestionById_ReturnsCorrectSuggestion()
        {
            var id = SeedSuggestion();

            var response = await _client.GetAsync($"/api/Suggestion/{id}");
            var returned = await response.Content.ReadFromJsonAsync<SuggestionDTO>();

            Assert.NotNull(returned);
            Assert.Equal(id, returned!.Id);
        }

        [Fact]
        public async Task CreateSuggestion_ReturnsCreated()
        {
            var userId = SeedAnonymousUser();
            var dto = new SuggestionCreationDTO
            {
                Title           = "Nova sugestija",
                Description     = "Opis sugestije",
                SuggestionBoxId = Guid.NewGuid(),
                AnonymousUserId = userId,
                CategoryIds     = new List<Guid>()
            };

            var response = await _client.PostAsJsonAsync("/api/Suggestion", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateSuggestion_ReturnsCorrectData()
        {
            var userId = SeedAnonymousUser();
            var dto = new SuggestionCreationDTO
            {
                Title           = "Kreirana sugestija",
                Description     = "Opis",
                SuggestionBoxId = Guid.NewGuid(),
                AnonymousUserId = userId,
                CategoryIds     = new List<Guid>()
            };

            var response = await _client.PostAsJsonAsync("/api/Suggestion", dto);
            var returned = await response.Content.ReadFromJsonAsync<SuggestionCreatedDTO>();

            Assert.NotNull(returned);
            Assert.Equal("Kreirana sugestija", returned!.Title);
            Assert.NotEqual(Guid.Empty, returned.Id);
        }

        [Fact]
        public async Task DeleteSuggestion_ReturnsNoContent()
        {
            var id = SeedSuggestion();

            var response = await _client.DeleteAsync($"/api/Suggestion/{id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    public class SuggestionCommentIntegrationTests : IClassFixture<SuggestionServiceWebAppFactory>
    {
        private readonly HttpClient _client;
        private readonly SuggestionServiceWebAppFactory _factory;

        public SuggestionCommentIntegrationTests(SuggestionServiceWebAppFactory factory)
        {
            _factory = factory;
            _client  = factory.CreateClient();
        }

        private (Guid userId, Guid suggestionId) SeedSuggestionWithUser()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SuggestionContext>();
            var link = new BoxAccessLink { Id = Guid.NewGuid(), AccessToken = "token", IsActive = true, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(7) };
            context.Set<BoxAccessLink>().Add(link);
            var user = new AnonymousUser { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, BoxAccessLinkId = link.Id };
            var suggestion = new Suggestion { Id = Guid.NewGuid(), Title = "Test", Description = "Opis", CreatedAt = DateTime.UtcNow, SuggestionBoxId = Guid.NewGuid(), AnonymousUserId = user.Id, Status = ProblemSuggestionStatus.Active };
            context.Suggestions.Add(suggestion);
            context.SaveChanges();
            return (user.Id, suggestion.Id);
        }

        [Fact]
        public async Task GetAllSuggestionComments_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/SuggestionComment");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetSuggestionCommentsBySuggestionId_ReturnsOk()
        {
            var response = await _client.GetAsync($"/api/SuggestionComment/suggestion/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateSuggestionComment_ReturnsCreated()
        {
            var (userId, suggestionId) = SeedSuggestionWithUser();
            var dto = new SuggestionCommentCreationDTO
            {
                Text              = "Test komentar",
                IsAnonymous       = false,
                SuggestionId      = suggestionId,
                SuggestionCommentId = null,
                CommentAuthorId   = userId,
                  CreatedBy = "TestUser"
            };

            var response = await _client.PostAsJsonAsync("/api/SuggestionComment", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task DeleteSuggestionComment_ReturnsNoContent()
        {
            var response = await _client.DeleteAsync($"/api/SuggestionComment/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    public class VoteIntegrationTests : IClassFixture<SuggestionServiceWebAppFactory>
    {
        private readonly HttpClient _client;
        private readonly SuggestionServiceWebAppFactory _factory;

        public VoteIntegrationTests(SuggestionServiceWebAppFactory factory)
        {
            _factory = factory;
            _client  = factory.CreateClient();
        }

        private (Guid userId, Guid suggestionId) SeedSuggestionWithUser()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SuggestionContext>();
            var link = new BoxAccessLink { Id = Guid.NewGuid(), AccessToken = "token", IsActive = true, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(7) };
            context.Set<BoxAccessLink>().Add(link);
            var user = new AnonymousUser { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, BoxAccessLinkId = link.Id };
            var suggestion = new Suggestion { Id = Guid.NewGuid(), Title = "Test", Description = "Opis", CreatedAt = DateTime.UtcNow, SuggestionBoxId = Guid.NewGuid(), AnonymousUserId = user.Id, Status = ProblemSuggestionStatus.Active };
            context.Suggestions.Add(suggestion);
            context.SaveChanges();
            return (user.Id, suggestion.Id);
        }

        [Fact]
        public async Task GetVotes_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/Vote");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetVotesBySuggestionId_ReturnsOk()
        {
            var response = await _client.GetAsync($"/api/Vote/suggestion/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateVote_ReturnsCreated()
        {
            var (userId, suggestionId) = SeedSuggestionWithUser();
            var dto = new VoteCreationDTO
            {
                VoteAuthorId = userId,
                SuggestionId = suggestionId
            };

            var response = await _client.PostAsJsonAsync("/api/Vote", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task DeleteVote_ReturnsNoContent()
        {
            var response = await _client.DeleteAsync($"/api/Vote/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
