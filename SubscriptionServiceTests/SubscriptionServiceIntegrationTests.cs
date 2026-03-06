using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SubscriptionService.Context;
using SubscriptionService.Models;
using SubscriptionService.Models.DTOs;
using SubscriptionService.ServiceCalls;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace SubscriptionService.Tests.Integration
{
    public class SubscriptionServiceWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<SubscriptionContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<SubscriptionContext>(options =>
                    options.UseInMemoryDatabase("SubscriptionIntegrationTestDb"));

                var billingDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(BillingServiceCall));
                if (billingDescriptor != null)
                    services.Remove(billingDescriptor);

                var httpClientDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IHttpClientFactory));
                if (httpClientDescriptor != null)
                    services.Remove(httpClientDescriptor);

                var mockFactory = new Mock<IHttpClientFactory>();
                var fakeHandler = new FakeHttpMessageHandler();
                var fakeClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-billing-service") };
                mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(fakeClient);
                services.AddSingleton<IHttpClientFactory>(mockFactory.Object);
                services.AddScoped<BillingServiceCall>();
            });

            builder.UseEnvironment("Testing");
        }
    }

    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    public class SubscriptionPlanIntegrationTests : IClassFixture<SubscriptionServiceWebAppFactory>
    {
        private readonly HttpClient _client;
        private readonly SubscriptionServiceWebAppFactory _factory;

        public SubscriptionPlanIntegrationTests(SubscriptionServiceWebAppFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private Guid SeedSubscriptionPlan(string title = "Basic Plan", string description = "Osnovni plan")
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SubscriptionContext>();
            var plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description
            };
            context.SubscriptionPlans.Add(plan);
            context.SaveChanges();
            return plan.Id;
        }

        [Fact]
        public async Task GetAllSubscriptionPlans_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/SubscriptionPlan");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAllSubscriptionPlans_ReturnsJsonContentType()
        {
            var response = await _client.GetAsync("/api/SubscriptionPlan");

            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task GetAllSubscriptionPlans_ReturnsList()
        {
            SeedSubscriptionPlan("Plan A");
            SeedSubscriptionPlan("Plan B");

            var response = await _client.GetAsync("/api/SubscriptionPlan");
            var returned = await response.Content.ReadFromJsonAsync<IEnumerable<SubscriptionPlanDTO>>();

            Assert.NotNull(returned);
        }

        [Fact]
        public async Task GetSubscriptionPlanById_ReturnsOk_WhenFound()
        {
            var id = SeedSubscriptionPlan("Get By Id Plan");

            var response = await _client.GetAsync($"/api/SubscriptionPlan/{id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetSubscriptionPlanById_ReturnsCorrectPlan()
        {
            var id = SeedSubscriptionPlan("Specific Plan", "Opis plana");

            var response = await _client.GetAsync($"/api/SubscriptionPlan/{id}");
            var returned = await response.Content.ReadFromJsonAsync<SubscriptionPlanDTO>();

            Assert.NotNull(returned);
            Assert.Equal(id, returned!.Id);
            Assert.Equal("Specific Plan", returned.Title);
        }

        [Fact]
        public async Task GetSubscriptionPlanById_ReturnsNotFound_WhenNotFound()
        {
            var response = await _client.GetAsync($"/api/SubscriptionPlan/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    public class SubscriptionIntegrationTests : IClassFixture<SubscriptionServiceWebAppFactory>
    {
        private readonly HttpClient _client;
        private readonly SubscriptionServiceWebAppFactory _factory;

        public SubscriptionIntegrationTests(SubscriptionServiceWebAppFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private Guid SeedSubscriptionPlan()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SubscriptionContext>();
            var plan = new SubscriptionPlan { Id = Guid.NewGuid(), Title = "Test Plan", Description = "Opis" };
            context.SubscriptionPlans.Add(plan);
            context.SaveChanges();
            return plan.Id;
        }

        private Guid SeedSubscription()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SubscriptionContext>();
            var planId = SeedSubscriptionPlan();
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                SubscriptionPlanId = planId,
                OrganizationId = Guid.NewGuid()
            };
            context.Subscriptions.Add(subscription);
            context.SaveChanges();
            return subscription.Id;
        }

        [Fact]
        public async Task GetAllSubscriptions_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/Subscription");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAllSubscriptions_ReturnsJsonContentType()
        {
            var response = await _client.GetAsync("/api/Subscription");

            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task GetSubscriptionById_ReturnsOk_WhenFound()
        {
            var id = SeedSubscription();

            var response = await _client.GetAsync($"/api/Subscription/{id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetSubscriptionById_ReturnsCorrectSubscription()
        {
            var id = SeedSubscription();

            var response = await _client.GetAsync($"/api/Subscription/{id}");
            var returned = await response.Content.ReadFromJsonAsync<SubscriptionDTO>();

            Assert.NotNull(returned);
            Assert.Equal(id, returned!.Id);
        }

        [Fact]
        public async Task GetSubscriptionById_ReturnsNotFound_WhenNotFound()
        {
            var response = await _client.GetAsync($"/api/Subscription/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateSubscription_ReturnsCreated()
        {
            var planId = SeedSubscriptionPlan();
            var dto = new SubscriptionCreationDTO
            {
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                SubscriptionPlanId = planId,
                OrganizationId = Guid.NewGuid()
            };

            var response = await _client.PostAsJsonAsync("/api/Subscription", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateSubscription_ReturnsCorrectData()
        {
            var planId = SeedSubscriptionPlan();
            var startDate = DateTime.UtcNow;
            var endDate = DateTime.UtcNow.AddMonths(1);
            var dto = new SubscriptionCreationDTO
            {
                StartDate = startDate,
                EndDate = endDate,
                SubscriptionPlanId = planId,
                OrganizationId = Guid.NewGuid()
            };

            var response = await _client.PostAsJsonAsync("/api/Subscription", dto);
            var returned = await response.Content.ReadFromJsonAsync<SubscriptionCreatedDTO>();

            Assert.NotNull(returned);
            Assert.NotEqual(Guid.Empty, returned!.Id);
        }

        [Fact]
        public async Task UpdateSubscription_ReturnsOk()
        {
            var id = SeedSubscription();
            var planId = SeedSubscriptionPlan();
            var updateDto = new SubscriptionDTO
            {
                Id = id,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(2),
                SubscriptionPlanId = planId,
                OrganizationId = Guid.NewGuid()
            };

            var response = await _client.PutAsJsonAsync("/api/Subscription", updateDto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateSubscription_ReturnsNotFound_WhenNotFound()
        {
            var updateDto = new SubscriptionDTO
            {
                Id = Guid.NewGuid(),
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                SubscriptionPlanId = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid()
            };

            var response = await _client.PutAsJsonAsync("/api/Subscription", updateDto);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteSubscription_ReturnsNoContent()
        {
            var id = SeedSubscription();

            var response = await _client.DeleteAsync($"/api/Subscription/{id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteSubscription_ReturnsNoContent_WhenNotFound()
        {
            var response = await _client.DeleteAsync($"/api/Subscription/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    public class PaymentIntegrationTests : IClassFixture<SubscriptionServiceWebAppFactory>
    {
        private readonly HttpClient _client;
        private readonly SubscriptionServiceWebAppFactory _factory;

        public PaymentIntegrationTests(SubscriptionServiceWebAppFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private Guid SeedSubscription()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SubscriptionContext>();
            var plan = new SubscriptionPlan { Id = Guid.NewGuid(), Title = "Plan", Description = "Opis" };
            context.SubscriptionPlans.Add(plan);
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                SubscriptionPlanId = plan.Id,
                OrganizationId = Guid.NewGuid()
            };
            context.Subscriptions.Add(subscription);
            context.SaveChanges();
            return subscription.Id;
        }

        [Fact]
        public async Task GetAllPayments_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/Payment");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAllPayments_ReturnsJsonContentType()
        {
            var response = await _client.GetAsync("/api/Payment");

            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task GetPaymentById_ReturnsNotFound_WhenNotFound()
        {
            var response = await _client.GetAsync($"/api/Payment/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreatePayment_ReturnsCreated()
        {
            var subscriptionId = SeedSubscription();
            var dto = new PaymentCreationDTO
            {
                SubscriptionId = subscriptionId,
                Total = 99.99,
                Currency = "USD",
                PaymentMethod = "CreditCard"
            };

            var response = await _client.PostAsJsonAsync("/api/Payment", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreatePayment_ReturnsCorrectData()
        {
            var subscriptionId = SeedSubscription();
            var dto = new PaymentCreationDTO
            {
                SubscriptionId = subscriptionId,
                Total = 49.99,
                Currency = "EUR",
                PaymentMethod = "PayPal"
            };

            var response = await _client.PostAsJsonAsync("/api/Payment", dto);
            var returned = await response.Content.ReadFromJsonAsync<PaymentCreatedDTO>();

            Assert.NotNull(returned);
            Assert.Equal(49.99, returned!.Total);
            Assert.NotEqual(Guid.Empty, returned.Id);
        }

        [Fact]
        public async Task GetPaymentById_ReturnsOk_WhenFound()
        {
            var subscriptionId = SeedSubscription();
            var createDto = new PaymentCreationDTO
            {
                SubscriptionId = subscriptionId,
                Total = 29.99,
                Currency = "USD",
                PaymentMethod = "CreditCard"
            };
            var createResponse = await _client.PostAsJsonAsync("/api/Payment", createDto);
            var created = await createResponse.Content.ReadFromJsonAsync<PaymentCreatedDTO>();

            var response = await _client.GetAsync($"/api/Payment/{created!.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetPaymentsBySubscriptionId_ReturnsOk()
        {
            var subscriptionId = SeedSubscription();

            var response = await _client.GetAsync($"/api/Payment/bySubscription/{subscriptionId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdatePayment_ReturnsNotFound_WhenNotFound()
        {
            var updateDto = new PaymentDTO
            {
                Id = Guid.NewGuid(),
                Total = 99.99,
                SubscriptionId = Guid.NewGuid(),
                Status = "Pending",
                Currency = "USD",
                PaymentMethod = "CreditCard"
            };

            var response = await _client.PutAsJsonAsync("/api/Payment", updateDto);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeletePayment_ReturnsNoContent()
        {
            var subscriptionId = SeedSubscription();
            var createDto = new PaymentCreationDTO
            {
                SubscriptionId = subscriptionId,
                Total = 19.99,
                Currency = "USD",
                PaymentMethod = "CreditCard"
            };
            var createResponse = await _client.PostAsJsonAsync("/api/Payment", createDto);
            var created = await createResponse.Content.ReadFromJsonAsync<PaymentCreatedDTO>();

            var response = await _client.DeleteAsync($"/api/Payment/{created!.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}

