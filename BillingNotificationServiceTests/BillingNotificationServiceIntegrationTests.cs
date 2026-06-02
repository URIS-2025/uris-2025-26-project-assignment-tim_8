using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AnonymousDomain.Models.BillingNotification;
using BillingNotificationService.Context;
using BillingNotificationService.Models.DTOs.BillingNotificationDTO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BillingNotificationService.Tests.Integration
{
    public class BillingNotificationServiceWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<BillingNotificationContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<BillingNotificationContext>(options =>
                    options.UseInMemoryDatabase("BillingNotificationIntegrationTestDb"));
            });

            builder.UseEnvironment("Testing");
        }
    }

    public class BillingNotificationIntegrationTests : IClassFixture<BillingNotificationServiceWebAppFactory>
    {
        private readonly HttpClient _client;
        private readonly BillingNotificationServiceWebAppFactory _factory;

        public BillingNotificationIntegrationTests(BillingNotificationServiceWebAppFactory factory)
        {
            _factory = factory;
            _client  = factory.CreateClient();
            // Controller is [Authorize]d — present a valid system-issued bearer for the CRUD tests.
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TestJwt.Create());
        }

        private Guid SeedNotification(string text = "Test notifikacija", bool isRead = false)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BillingNotificationContext>();
            var notification = new BillingNotification
            {
                Id             = Guid.NewGuid(),
                Text           = text,
                IsRead         = isRead,
                OrganizationId = Guid.NewGuid(),
                PaymentId      = Guid.NewGuid()
            };
            context.BillingNotifications.Add(notification);
            context.SaveChanges();
            return notification.Id;
        }

        // ─── GET ALL ──────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllBillingNotifications_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/BillingNotification");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAllBillingNotifications_ReturnsJsonContentType()
        {
            var response = await _client.GetAsync("/api/BillingNotification");

            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task GetAllBillingNotifications_ReturnsList()
        {
            SeedNotification("Notif 1");
            SeedNotification("Notif 2");

            var response = await _client.GetAsync("/api/BillingNotification");
            var returned = await response.Content.ReadFromJsonAsync<IEnumerable<BillingNotificationDTO>>();

            Assert.NotNull(returned);
        }

        // ─── GET BY ID ────────────────────────────────────────────────────────

        [Fact]
        public async Task GetBillingNotificationById_ReturnsOk_WhenFound()
        {
            var id = SeedNotification("Get by id test");

            var response = await _client.GetAsync($"/api/BillingNotification/{id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetBillingNotificationById_ReturnsCorrectNotification()
        {
            var id = SeedNotification("Specific notifikacija");

            var response = await _client.GetAsync($"/api/BillingNotification/{id}");
            var returned = await response.Content.ReadFromJsonAsync<BillingNotificationDTO>();

            Assert.NotNull(returned);
            Assert.Equal(id, returned!.Id);
            Assert.Equal("Specific notifikacija", returned.Text);
        }

        [Fact]
        public async Task GetBillingNotificationById_ReturnsOk_WithNull_WhenNotFound()
        {
            var response = await _client.GetAsync($"/api/BillingNotification/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        // ─── CREATE ───────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateBillingNotification_ReturnsCreated()
        {
            var dto = new BillingNotificationCreationDTO
            {
                Text           = "Nova notifikacija",
                OrganizationId = Guid.NewGuid(),
                PaymentId      = Guid.NewGuid()
            };

            var response = await _client.PostAsJsonAsync("/api/BillingNotification", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateBillingNotification_ReturnsCorrectData()
        {
            var orgId     = Guid.NewGuid();
            var paymentId = Guid.NewGuid();
            var dto = new BillingNotificationCreationDTO
            {
                Text           = "Kreirana notifikacija",
                OrganizationId = orgId,
                PaymentId      = paymentId
            };

            var response = await _client.PostAsJsonAsync("/api/BillingNotification", dto);
            var returned = await response.Content.ReadFromJsonAsync<BillingNotificationCreatedDTO>();

            Assert.NotNull(returned);
            Assert.Equal("Kreirana notifikacija", returned!.Text);
            Assert.Equal(orgId,     returned.OrganizationId);
            Assert.Equal(paymentId, returned.PaymentId);
            Assert.NotEqual(Guid.Empty, returned.Id);
        }

        // ─── UPDATE ───────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateBillingNotification_ReturnsOk()
        {
            var id = SeedNotification("Stari tekst");
            var updateDto = new BillingNotificationUpdateDTO
            {
                Id             = id,
                Text           = "Novi tekst",
                OrganizationId = Guid.NewGuid(),
                PaymentId      = Guid.NewGuid()
            };

            var response = await _client.PutAsJsonAsync("/api/BillingNotification", updateDto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateBillingNotification_UpdatesText()
        {
            var id = SeedNotification("Prije update");
            var updateDto = new BillingNotificationUpdateDTO
            {
                Id             = id,
                Text           = "Nakon update",
                OrganizationId = Guid.NewGuid(),
                PaymentId      = Guid.NewGuid()
            };

            await _client.PutAsJsonAsync("/api/BillingNotification", updateDto);

            var getResponse = await _client.GetAsync($"/api/BillingNotification/{id}");
            var returned    = await getResponse.Content.ReadFromJsonAsync<BillingNotificationDTO>();

            Assert.Equal("Nakon update", returned!.Text);
        }

        // ─── DELETE ───────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteBillingNotification_ReturnsNoContent_WhenFound()
        {
            var id = SeedNotification("Za brisanje");

            var response = await _client.DeleteAsync($"/api/BillingNotification/{id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteBillingNotification_ReturnsNoContent_WhenNotFound()
        {
            var response = await _client.DeleteAsync($"/api/BillingNotification/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
