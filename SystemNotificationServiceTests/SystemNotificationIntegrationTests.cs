using AnonymousDomain.Models.SystemNotification;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using SystemNotificationService.Context;
using SystemNotificationService.Models.DTOs.SystemNotification;
using Xunit;

namespace SystemNotificationService.Tests.Integration
{
    public class SystemNotificationServiceWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<SystemNotificationContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<SystemNotificationContext>(options =>
                    options.UseInMemoryDatabase("SystemNotificationIntegrationTestDb"));
            });

            builder.UseEnvironment("Testing");
        }
    }

    public class SystemNotificationIntegrationTests : IClassFixture<SystemNotificationServiceWebAppFactory>
    {
        private readonly HttpClient _client;
        private readonly SystemNotificationServiceWebAppFactory _factory;

        public SystemNotificationIntegrationTests(SystemNotificationServiceWebAppFactory factory)
        {
            _factory = factory;
            _client  = factory.CreateClient();
        }

        private Guid SeedNotification(string text = "Test", Guid? problemCommentId = null, Guid? suggestionCommentId = null)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SystemNotificationContext>();
            var notification = new SystemNotification
            {
                Id                  = Guid.NewGuid(),
                Text                = text,
                IsRead              = false,
                CreatedAt           = DateTime.UtcNow,
                UpdatedAt           = DateTime.UtcNow,
                OrganizationId      = Guid.NewGuid(),
                AnonymousUserId     = Guid.NewGuid(),
                ProblemCommentId    = problemCommentId,
                SuggestionCommentId = suggestionCommentId
            };
            context.SystemNotifications.Add(notification);
            context.SaveChanges();
            return notification.Id;
        }

        // ─── GET ALL ──────────────────────────────────────────────────────────

        [Fact]
        public async Task GetNotifications_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/SystemNotification");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetNotifications_ReturnsJsonContentType()
        {
            var response = await _client.GetAsync("/api/SystemNotification");

            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task GetNotifications_ReturnsEmptyList()
        {
            var response = await _client.GetAsync("/api/SystemNotification");
            var returned = await response.Content.ReadFromJsonAsync<IEnumerable<SystemNotificationCreatedDTO>>();

            Assert.NotNull(returned);
            Assert.Empty(returned!);
        }

        // ─── CREATE ───────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateSystemNotification_ReturnsCreated_WithProblemCommentId()
        {
            var dto = new SystemNotificationCreationDTO
            {
                Text                = "Integration problem notif",
                OrganizationId      = Guid.NewGuid(),
                AnonymousUserId     = Guid.NewGuid(),
                ProblemCommentId    = Guid.NewGuid(),
                SuggestionCommentId = null
            };

            var response = await _client.PostAsJsonAsync("/api/SystemNotification", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateSystemNotification_ReturnsCreated_WithSuggestionCommentId()
        {
            var dto = new SystemNotificationCreationDTO
            {
                Text                = "Integration suggestion notif",
                OrganizationId      = Guid.NewGuid(),
                AnonymousUserId     = Guid.NewGuid(),
                ProblemCommentId    = null,
                SuggestionCommentId = Guid.NewGuid()
            };

            var response = await _client.PostAsJsonAsync("/api/SystemNotification", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateSystemNotification_ReturnsCorrectData()
        {
            var problemId = Guid.NewGuid();
            var dto = new SystemNotificationCreationDTO
            {
                Text             = "Kreirana notifikacija",
                OrganizationId   = Guid.NewGuid(),
                ProblemCommentId = problemId
            };

            var response = await _client.PostAsJsonAsync("/api/SystemNotification", dto);
            var returned = await response.Content.ReadFromJsonAsync<SystemNotificationCreatedDTO>();

            Assert.Equal("Kreirana notifikacija", returned!.Text);
            Assert.Equal(problemId, returned.ProblemCommentId);
            Assert.NotEqual(Guid.Empty, returned.Id);
        }

        // ─── DELETE ───────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteSystemNotification_ReturnsNoContent_WhenFound()
        {
            var id = SeedNotification("Za brisanje", problemCommentId: Guid.NewGuid());

            var response = await _client.DeleteAsync($"/api/SystemNotification/{id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteSystemNotification_ReturnsBadRequest_WhenNotFound()
        {
            var response = await _client.DeleteAsync($"/api/SystemNotification/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }
}
