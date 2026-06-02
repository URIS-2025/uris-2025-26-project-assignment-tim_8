using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace SystemNotificationService.Tests.Integration
{
    // Verifies the [Authorize] gate: an unauthenticated request is rejected with 401.
    // Reuses the integration WebApplicationFactory but deliberately sends NO bearer token.
    public class SystemNotificationAuthTests : IClassFixture<SystemNotificationServiceWebAppFactory>
    {
        private readonly HttpClient _client;

        public SystemNotificationAuthTests(SystemNotificationServiceWebAppFactory factory)
        {
            _client = factory.CreateClient(); // no Authorization header
        }

        [Fact]
        public async Task GetNotifications_WithoutToken_ReturnsUnauthorized()
        {
            var response = await _client.GetAsync("/api/SystemNotification");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateNotification_WithoutToken_ReturnsUnauthorized()
        {
            var response = await _client.PostAsJsonAsync("/api/SystemNotification", new { text = "x" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
