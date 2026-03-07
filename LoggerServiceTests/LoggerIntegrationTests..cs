using System.Net;
using System.Net.Http.Json;
using LoggerService.Context;
using LoggerService.Models.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Xunit;

namespace LoggerServiceIntegrationTests
{
    public class LoggerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;
        private readonly string _dbName = Guid.NewGuid().ToString();

        public LoggerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var descriptors = services.Where(
                        d => d.ServiceType == typeof(DbContextOptions<LoggerContext>) ||
                             d.ServiceType == typeof(DbContextOptions) ||
                             d.ServiceType == typeof(LoggerContext)).ToList();

                    foreach (var descriptor in descriptors)
                        services.Remove(descriptor);

                    services.AddDbContext<LoggerContext>((options) =>
                    {
                        options.UseInMemoryDatabase(_dbName);
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        public void Dispose()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LoggerContext>();
            db.Database.EnsureDeleted();
        }

        // Helper method
        private async Task<LogDTO> CreateTestLog()
        {
            var creationDTO = new LogCreationDTO
            {
                Action = "CREATE",
                ServiceName = "ProblemService",
                IsSuccess = true,
                HttpMethod = "POST"
            };
            var response = await _client.PostAsJsonAsync("/api/Logger", creationDTO);
            return await response.Content.ReadFromJsonAsync<LogDTO>();
        }

        // GET ALL
        [Fact]
        public async Task GetAll_ReturnsNoContent_WhenNoLogsExist()
        {
            var response = await _client.GetAsync("/api/Logger");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WhenLogsExist()
        {
            await CreateTestLog();

            var response = await _client.GetAsync("/api/Logger");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithCustomTake()
        {
            await CreateTestLog();

            var response = await _client.GetAsync("/api/Logger?take=10");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // GET BY ID
        [Fact]
        public async Task GetById_ReturnsOk_WhenLogExists()
        {
            var created = await CreateTestLog();

            var response = await _client.GetAsync($"/api/Logger/{created.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenLogDoesNotExist()
        {
            var response = await _client.GetAsync($"/api/Logger/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // SEARCH
        [Fact]
        public async Task Search_ReturnsOk_WhenLogsMatchServiceName()
        {
            await CreateTestLog();

            var response = await _client.GetAsync("/api/Logger/search?serviceName=ProblemService");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Search_ReturnsNoContent_WhenNoLogsMatch()
        {
            var response = await _client.GetAsync("/api/Logger/search?serviceName=NonExistentService");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Search_ReturnsOk_WithIsSuccessFilter()
        {
            await CreateTestLog();

            var response = await _client.GetAsync("/api/Logger/search?isSuccess=true");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Search_ReturnsOk_WithHttpMethodFilter()
        {
            await CreateTestLog();

            var response = await _client.GetAsync("/api/Logger/search?httpMethod=POST");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Search_ReturnsOk_WithDateRangeFilter()
        {
            await CreateTestLog();

            var fromUtc = DateTime.UtcNow.AddDays(-1).ToString("o");
            var toUtc = DateTime.UtcNow.AddDays(1).ToString("o");

            var response = await _client.GetAsync($"/api/Logger/search?fromUtc={fromUtc}&toUtc={toUtc}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Search_ReturnsNoContent_WithIsSuccessFalseFilter()
        {
            await CreateTestLog();

            var response = await _client.GetAsync("/api/Logger/search?isSuccess=false");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        // CREATE
        [Fact]
        public async Task Create_ReturnsCreated_WhenValidData()
        {
            var creationDTO = new LogCreationDTO
            {
                Action = "CREATE",
                ServiceName = "ProblemService",
                IsSuccess = true
            };

            var response = await _client.PostAsJsonAsync("/api/Logger", creationDTO);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Create_ReturnsCreated_WithAllOptionalFields()
        {
            var creationDTO = new LogCreationDTO
            {
                Action = "UPDATE",
                ServiceName = "ProblemService",
                UserId = "user123",
                EntityName = "Problem",
                OldValues = "{\"Title\":\"Old\"}",
                NewValues = "{\"Title\":\"New\"}",
                HttpMethod = "PUT",
                IsSuccess = true
            };

            var response = await _client.PostAsJsonAsync("/api/Logger", creationDTO);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Create_ReturnsCreated_WhenIsSuccessIsFalse()
        {
            var creationDTO = new LogCreationDTO
            {
                Action = "DELETE",
                ServiceName = "ProblemService",
                IsSuccess = false
            };

            var response = await _client.PostAsJsonAsync("/api/Logger", creationDTO);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        // DELETE
        [Fact]
        public async Task Delete_ReturnsNoContent_WhenLogExists()
        {
            var created = await CreateTestLog();

            var response = await _client.DeleteAsync($"/api/Logger/{created.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenLogDoesNotExist()
        {
            var response = await _client.DeleteAsync($"/api/Logger/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // OPTIONS
        [Fact]
        public async Task GetOptions_ReturnsOk()
        {
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/Logger");
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}