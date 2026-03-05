using System.Net;
using System.Net.Http.Json;
using AnonymousDomain.Models.AnonymousUser;
using AnonymousUserService.Context;
using AnonymousUserService.Models.DTOs.AnonymousUser;
using AnonymousUserService.Models.DTOs.BoxAccessLink;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Xunit;

namespace AnonymousUserService.Tests.Integration
{
    public class AnonymousUserServiceWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Ukloni postojeći DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AnonymousUserContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Dodaj InMemory bazu
                services.AddDbContext<AnonymousUserContext>(options =>
                    options.UseInMemoryDatabase("AnonymousUserIntegrationTestDb"));
            });

            builder.UseEnvironment("Testing");
        }
    }

    // ─── ANONYMOUS USER ENDPOINTS ─────────────────────────────────────────────

    public class AnonymousUserIntegrationTests : IClassFixture<AnonymousUserServiceWebAppFactory>
    {
        private readonly HttpClient _client;
        private readonly AnonymousUserServiceWebAppFactory _factory;

        public AnonymousUserIntegrationTests(AnonymousUserServiceWebAppFactory factory)
        {
            _factory = factory;
            _client  = factory.CreateClient();
        }

        // Pomocna metoda za seedovanje BoxAccessLink direktno u bazu
        private Guid SeedBoxAccessLink()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AnonymousUserContext>();
            var link = new BoxAccessLink
            {
                Id          = Guid.NewGuid(),
                AccessToken = "integration-token",
                IsActive    = true,
                CreatedAt   = DateTime.UtcNow,
                ExpiresAt   = DateTime.UtcNow.AddDays(7)
            };
            context.BoxAccessLinks.Add(link);
            context.SaveChanges();
            return link.Id;
        }

        private Guid SeedAnonymousUser()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AnonymousUserContext>();
            var linkId = SeedBoxAccessLink();
            var user = new AnonymousUser
            {
                Id              = Guid.NewGuid(),
                CreatedAt       = DateTime.UtcNow,
                BoxAccessLinkId = linkId
            };
            context.AnonymousUsers.Add(user);
            context.SaveChanges();
            return user.Id;
        }

        [Fact]
        public async Task GetAllAnonymousUsers_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/AnonymousUser");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAllAnonymousUsers_ReturnsJsonContentType()
        {
            var response = await _client.GetAsync("/api/AnonymousUser");

            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task GetAllAnonymousUsers_ReturnsList()
        {
            var response = await _client.GetAsync("/api/AnonymousUser");
            var returned = await response.Content.ReadFromJsonAsync<IEnumerable<AnonymousUserDTO>>();

            Assert.NotNull(returned);
        }

        [Fact]
        public async Task GetAnonymousUserById_ReturnsOk_WhenFound()
        {
            var userId = SeedAnonymousUser();

            var response = await _client.GetAsync($"/api/AnonymousUser/{userId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAnonymousUserById_ReturnsCorrectUser()
        {
            var userId = SeedAnonymousUser();

            var response = await _client.GetAsync($"/api/AnonymousUser/{userId}");
            var returned = await response.Content.ReadFromJsonAsync<AnonymousUserDTO>();

            Assert.NotNull(returned);
            Assert.Equal(userId, returned!.Id);
        }

        [Fact]
        public async Task GetAnonymousUserById_ReturnsOk_WithNull_WhenNotFound()
        {
            var response = await _client.GetAsync($"/api/AnonymousUser/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteAnonymousUser_ReturnsNoContent_WhenFound()
        {
            var userId = SeedAnonymousUser();

            var response = await _client.DeleteAsync($"/api/AnonymousUser/{userId}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteAnonymousUser_ReturnsNoContent_WhenNotFound()
        {
            // Repo ne baca exception nego ignorise
            var response = await _client.DeleteAsync($"/api/AnonymousUser/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    // ─── BOX ACCESS LINK ENDPOINTS ────────────────────────────────────────────

    public class BoxAccessLinkIntegrationTests : IClassFixture<AnonymousUserServiceWebAppFactory>
    {
        private readonly HttpClient _client;
        private readonly AnonymousUserServiceWebAppFactory _factory;

        public BoxAccessLinkIntegrationTests(AnonymousUserServiceWebAppFactory factory)
        {
            _factory = factory;
            _client  = factory.CreateClient();
        }

        private Guid SeedBoxAccessLink(string token = "test-token", bool isActive = true)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AnonymousUserContext>();
            var link = new BoxAccessLink
            {
                Id          = Guid.NewGuid(),
                AccessToken = token,
                IsActive    = isActive,
                CreatedAt   = DateTime.UtcNow,
                ExpiresAt   = DateTime.UtcNow.AddDays(7)
            };
            context.BoxAccessLinks.Add(link);
            context.SaveChanges();
            return link.Id;
        }

        [Fact]
        public async Task GetAllBoxAccessLinks_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/BoxAccessLink");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAllBoxAccessLinks_ReturnsJsonContentType()
        {
            var response = await _client.GetAsync("/api/BoxAccessLink");

            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task GetAllBoxAccessLinks_ReturnsList()
        {
            SeedBoxAccessLink("token-list-1");
            SeedBoxAccessLink("token-list-2");

            var response  = await _client.GetAsync("/api/BoxAccessLink");
            var returned  = await response.Content.ReadFromJsonAsync<IEnumerable<BoxAccessLinkDTO>>();

            Assert.NotNull(returned);
        }

        [Fact]
        public async Task GetBoxAccessLinkById_ReturnsOk_WhenFound()
        {
            var linkId = SeedBoxAccessLink("specific-token");

            var response = await _client.GetAsync($"/api/BoxAccessLink/{linkId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetBoxAccessLinkById_ReturnsCorrectLink()
        {
            var linkId = SeedBoxAccessLink("verify-token", true);

            var response = await _client.GetAsync($"/api/BoxAccessLink/{linkId}");
            var returned = await response.Content.ReadFromJsonAsync<BoxAccessLinkDTO>();

            Assert.NotNull(returned);
            Assert.Equal(linkId, returned!.Id);
            Assert.Equal("verify-token", returned.AccessToken);
            Assert.True(returned.IsActive);
        }

        [Fact]
        public async Task GetBoxAccessLinkById_ReturnsOk_WithNull_WhenNotFound()
        {
            var response = await _client.GetAsync($"/api/BoxAccessLink/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
