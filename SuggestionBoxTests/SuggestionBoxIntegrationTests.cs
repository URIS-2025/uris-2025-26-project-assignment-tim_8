using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SuggestionBoxService.Context;
using SuggestionBoxService.Models;
using SuggestionBoxService.Models.DTOs;
using Xunit;

namespace SuggestionBoxServiceTests.Integration
{
    public class SuggestionBoxServiceWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "ConnectionStrings:SuggestionBoxDB", "Server=fake;Database=fake;" },
                    { "Services:OrganizationService", "http://localhost:5000/" }
                });
            });

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<SuggestionBoxContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                var dbDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(SuggestionBoxContext));
                if (dbDescriptor != null)
                    services.Remove(dbDescriptor);

                services.AddDbContext<SuggestionBoxContext>(options =>
                    options.UseInMemoryDatabase("SuggestionBoxIntegrationTestDb"));
            });

            builder.UseEnvironment("Testing");
        }
    }

    public class SuggestionBoxIntegrationTests : IClassFixture<SuggestionBoxServiceWebAppFactory>
    {
        private readonly HttpClient _client;
        private readonly SuggestionBoxServiceWebAppFactory _factory;

        public SuggestionBoxIntegrationTests(SuggestionBoxServiceWebAppFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private Guid SeedSuggestionBox(Guid? organizationId = null)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SuggestionBoxContext>();
            var box = new SuggestionBox
            {
                Id = Guid.NewGuid(),
                Name = "Test Box",
                Description = "Test Description",
                IsDarkTheme = false,
                CreatedAt = DateTime.UtcNow,
                Password = "test123",
                CreatedBy = "TestUser",
                OrganizationId = organizationId ?? Guid.NewGuid(),
                BoxAccessLinkId = Guid.NewGuid()
            };
            context.SuggestionBoxes.Add(box);
            context.SaveChanges();
            return box.Id;
        }

        [Fact]
        public async Task GetAllSuggestionBoxes_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/SuggestionBox");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAllSuggestionBoxes_ReturnsJsonContentType()
        {
            var response = await _client.GetAsync("/api/SuggestionBox");
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task GetAllSuggestionBoxes_ReturnsList()
        {
            var response = await _client.GetAsync("/api/SuggestionBox");
            var returned = await response.Content.ReadFromJsonAsync<IEnumerable<SuggestionBoxDTO>>();
            Assert.NotNull(returned);
        }

        [Fact]
        public async Task GetSuggestionBoxById_ReturnsOk_WhenFound()
        {
            var id = SeedSuggestionBox();
            var response = await _client.GetAsync($"/api/SuggestionBox/{id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetSuggestionBoxById_ReturnsCorrectBox()
        {
            var id = SeedSuggestionBox();
            var response = await _client.GetAsync($"/api/SuggestionBox/{id}");
            var returned = await response.Content.ReadFromJsonAsync<SuggestionBoxDTO>();
            Assert.NotNull(returned);
            Assert.Equal(id, returned!.Id);
        }

        [Fact]
        public async Task GetSuggestionBoxById_ReturnsNotFound_WhenNotExists()
        {
            var response = await _client.GetAsync($"/api/SuggestionBox/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetByOrganizationId_ReturnsOk()
        {
            var organizationId = Guid.NewGuid();
            SeedSuggestionBox(organizationId: organizationId);
            var response = await _client.GetAsync($"/api/SuggestionBox/organization/{organizationId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetByOrganizationId_ReturnsCorrectBoxes()
        {
            var organizationId = Guid.NewGuid();
            SeedSuggestionBox(organizationId: organizationId);
            SeedSuggestionBox(organizationId: organizationId);
            var response = await _client.GetAsync($"/api/SuggestionBox/organization/{organizationId}");
            var returned = await response.Content.ReadFromJsonAsync<IEnumerable<SuggestionBoxDTO>>();
            Assert.NotNull(returned);
            Assert.Equal(2, returned!.Count());
        }

        [Fact]
        public async Task GetByOrganizationId_ReturnsEmpty_WhenNoneExist()
        {
            var response = await _client.GetAsync($"/api/SuggestionBox/organization/{Guid.NewGuid()}");
            var returned = await response.Content.ReadFromJsonAsync<IEnumerable<SuggestionBoxDTO>>();
            Assert.NotNull(returned);
            Assert.Empty(returned!);
        }

        [Fact]
        public async Task CreateSuggestionBox_ReturnsCreated()
        {
            var createDto = new SuggestionBoxCreateDTO
            {
                Name = "New Box",
                Description = "Description",
                IsDarkTheme = false,
                Password = "pass123",
                CreatedBy = "TestUser",
                OrganizationId = Guid.NewGuid()
            };
            var response = await _client.PostAsJsonAsync("/api/SuggestionBox", createDto);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateSuggestionBox_ReturnsCorrectData()
        {
            var createDto = new SuggestionBoxCreateDTO
            {
                Name = "New Box",
                Description = "Description",
                IsDarkTheme = false,
                Password = "pass123",
                CreatedBy = "TestUser",
                OrganizationId = Guid.NewGuid()
            };
            var response = await _client.PostAsJsonAsync("/api/SuggestionBox", createDto);
            var returned = await response.Content.ReadFromJsonAsync<SuggestionBoxDTO>();
            Assert.NotNull(returned);
            Assert.Equal("New Box", returned!.Name);
        }

        [Fact]
        public async Task UpdateSuggestionBox_ReturnsOk()
        {
            var id = SeedSuggestionBox();
            var updateDto = new SuggestionBoxUpdateDTO
            {
                Id = id,
                Name = "Updated Box",
                Description = "Updated Description",
                IsDarkTheme = true,
                Password = "newpass"
            };
            var response = await _client.PutAsJsonAsync("/api/SuggestionBox", updateDto);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateSuggestionBox_ReturnsUpdatedData()
        {
            var id = SeedSuggestionBox();
            var updateDto = new SuggestionBoxUpdateDTO
            {
                Id = id,
                Name = "Updated Box",
                Description = "Updated Description",
                IsDarkTheme = true,
                Password = "newpass"
            };
            var response = await _client.PutAsJsonAsync("/api/SuggestionBox", updateDto);
            var returned = await response.Content.ReadFromJsonAsync<SuggestionBoxDTO>();
            Assert.NotNull(returned);
            Assert.Equal("Updated Box", returned!.Name);
        }

        [Fact]
        public async Task UpdateSuggestionBox_ReturnsNotFound_WhenNotExists()
        {
            var updateDto = new SuggestionBoxUpdateDTO
            {
                Id = Guid.NewGuid(),
                Name = "Updated Box",
                Description = "Updated Description",
                IsDarkTheme = true,
                Password = "newpass"
            };
            var response = await _client.PutAsJsonAsync("/api/SuggestionBox", updateDto);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteSuggestionBox_ReturnsNoContent()
        {
            var id = SeedSuggestionBox();
            var response = await _client.DeleteAsync($"/api/SuggestionBox/{id}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteSuggestionBox_ReturnsNoContent_WhenNotFound()
        {
            var response = await _client.DeleteAsync($"/api/SuggestionBox/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteByOrganizationId_ReturnsNoContent()
        {
            var organizationId = Guid.NewGuid();
            SeedSuggestionBox(organizationId: organizationId);
            SeedSuggestionBox(organizationId: organizationId);
            var response = await _client.DeleteAsync($"/api/SuggestionBox/organization/{organizationId}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteByOrganizationId_ReturnsNoContent_WhenNoneExist()
        {
            var response = await _client.DeleteAsync($"/api/SuggestionBox/organization/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
