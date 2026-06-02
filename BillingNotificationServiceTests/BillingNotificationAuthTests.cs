using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BillingNotificationService.Tests.Integration
{
    // Verifies the [Authorize] gate: an unauthenticated request is rejected with 401.
    // Reuses the integration WebApplicationFactory but deliberately sends NO bearer token.
    public class BillingNotificationAuthTests : IClassFixture<BillingNotificationServiceWebAppFactory>
    {
        private readonly HttpClient _client;

        public BillingNotificationAuthTests(BillingNotificationServiceWebAppFactory factory)
        {
            _client = factory.CreateClient(); // no Authorization header
        }

        [Fact]
        public async Task GetAllBillingNotifications_WithoutToken_ReturnsUnauthorized()
        {
            var response = await _client.GetAsync("/api/BillingNotification");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateBillingNotification_WithoutToken_ReturnsUnauthorized()
        {
            var response = await _client.PostAsJsonAsync("/api/BillingNotification",
                new { text = "x", organizationId = System.Guid.NewGuid(), paymentId = System.Guid.NewGuid() });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
