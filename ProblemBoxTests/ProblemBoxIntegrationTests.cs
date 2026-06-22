using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProblemBoxService.Context;
using ProblemBoxService.Enums;
using ProblemBoxService.Models.DTOs;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ProblemBoxServiceIntegrationTests
{
    public class ProblemBoxIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;

        public ProblemBoxIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Remove all DbContext registrations
                    var descriptors = services.Where(
                        d => d.ServiceType == typeof(DbContextOptions<ProblemBoxContext>) ||
                             d.ServiceType == typeof(DbContextOptions) ||
                             d.ServiceType == typeof(ProblemBoxContext)).ToList();

                    foreach (var descriptor in descriptors)
                        services.Remove(descriptor);

                    // Add InMemory database
                    services.AddDbContext<ProblemBoxContext>((options) =>
                    {
                        options.UseInMemoryDatabase("TestProblemBoxDb");
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        // GET ALL
        [Fact]
        public async Task GetProblemBoxes_ReturnsOkResponse()
        {
            var response = await _client.GetAsync("/api/ProblemBox");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // GET BY ORGANIZATION ID
        [Fact]
        public async Task GetProblemBoxByOrganizationId_ReturnsOkResponse_WithList()
        {
            var organizationId = Guid.NewGuid();
            var creationDTO = new ProblemBoxCreationDTO
            {
                Name = "Test Box",
                Description = "Test Description",
                Password = "password123",
                CreatedBy = "TestUser",
                OrganizationId = organizationId,
                IsDarkTheme = false,
            };
            await _client.PostAsJsonAsync("/api/ProblemBox", creationDTO);

            var response = await _client.GetAsync($"/api/ProblemBox/organization/{organizationId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetProblemBoxByOrganizationId_ReturnsOkResponse_WithEmptyList()
        {
            var response = await _client.GetAsync($"/api/ProblemBox/organization/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // GET BY ID
        [Fact]
        public async Task GetProblemBoxById_ReturnsOk_WhenProblemBoxExists()
        {
            var creationDTO = new ProblemBoxCreationDTO
            {
                Name = "Test Box",
                Description = "Test Description",
                Password = "password123",
                CreatedBy = "TestUser",
                OrganizationId = Guid.NewGuid(),
                IsDarkTheme = false,
            };
            var createResponse = await _client.PostAsJsonAsync("/api/ProblemBox", creationDTO);
            var created = await createResponse.Content.ReadFromJsonAsync<ProblemBoxCreatedDTO>();

            var response = await _client.GetAsync($"/api/ProblemBox/{created.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetProblemBoxById_ReturnsInternalServerError_WhenNotFound()
        {
            var response = await _client.GetAsync($"/api/ProblemBox/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // CREATE
        [Fact]
        public async Task CreateProblemBox_ReturnsCreated_WhenValidData()
        {
            var creationDTO = new ProblemBoxCreationDTO
            {
                Name = "Test Box",
                Description = "Test Description",
                Password = "password123",
                CreatedBy = "TestUser",
                OrganizationId = Guid.NewGuid(),
                IsDarkTheme = false,
            };

            var response = await _client.PostAsJsonAsync("/api/ProblemBox", creationDTO);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateProblemBox_ReturnsBadRequest_WhenOrganizationIdIsEmpty()
        {
            var creationDTO = new ProblemBoxCreationDTO
            {
                Name = "Test Box",
                Description = "Test Description",
                Password = "password123",
                CreatedBy = "TestUser",
                OrganizationId = Guid.Empty,
                IsDarkTheme = false,
            };

            var response = await _client.PostAsJsonAsync("/api/ProblemBox", creationDTO);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateProblemBox_ReturnsBadRequest_WhenNameIsEmpty()
        {
            var creationDTO = new ProblemBoxCreationDTO
            {
                Name = "",
                Description = "Test Description",
                Password = "password123",
                CreatedBy = "TestUser",
                OrganizationId = Guid.NewGuid(),
                IsDarkTheme = false,
            };

            var response = await _client.PostAsJsonAsync("/api/ProblemBox", creationDTO);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateProblemBox_ReturnsBadRequest_WhenDescriptionIsEmpty()
        {
            var creationDTO = new ProblemBoxCreationDTO
            {
                Name = "Test Box",
                Description = "",
                Password = "password123",
                CreatedBy = "TestUser",
                OrganizationId = Guid.NewGuid(),
                IsDarkTheme = false,
            };

            var response = await _client.PostAsJsonAsync("/api/ProblemBox", creationDTO);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateProblemBox_ReturnsCreated_WhenPasswordIsEmpty()
        {
            // Password is now OPTIONAL: an empty password creates a public (no-password) box.
            var creationDTO = new ProblemBoxCreationDTO
            {
                Name = "Test Box",
                Description = "Test Description",
                Password = "",
                CreatedBy = "TestUser",
                OrganizationId = Guid.NewGuid(),
                IsDarkTheme = false,
            };

            var response = await _client.PostAsJsonAsync("/api/ProblemBox", creationDTO);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        // UPDATE
        [Fact]
        public async Task UpdateProblemBox_ReturnsOk_WhenProblemBoxExists()
        {
            var creationDTO = new ProblemBoxCreationDTO
            {
                Name = "Test Box",
                Description = "Test Description",
                Password = "password123",
                CreatedBy = "TestUser",
                OrganizationId = Guid.NewGuid(),
                IsDarkTheme = false,
            };
            var createResponse = await _client.PostAsJsonAsync("/api/ProblemBox", creationDTO);
            var created = await createResponse.Content.ReadFromJsonAsync<ProblemBoxCreatedDTO>();

            var updateDTO = new ProblemBoxUpdateDTO
            {
                Id = created.Id,
                Name = "Updated Box",
                Description = "Updated Description",
                Password = "newpassword123",
                IsDarkTheme = true,
                Status = ProblemSuggestionStatus.Inactive
            };

            var response = await _client.PutAsJsonAsync("/api/ProblemBox", updateDTO);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProblemBox_ReturnsInternalServerError_WhenNotFound()
        {
            var updateDTO = new ProblemBoxUpdateDTO
            {
                Id = Guid.NewGuid(),
                Name = "Updated Box",
                Description = "Updated Description",
                Password = "newpassword123",
                IsDarkTheme = true,
                Status = ProblemSuggestionStatus.Active
            };

            var response = await _client.PutAsJsonAsync("/api/ProblemBox", updateDTO);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // DELETE
        [Fact]
        public async Task DeleteProblemBox_ReturnsNoContent_WhenExists()
        {
            var creationDTO = new ProblemBoxCreationDTO
            {
                Name = "Test Box",
                Description = "Test Description",
                Password = "password123",
                CreatedBy = "TestUser",
                OrganizationId = Guid.NewGuid(),
                IsDarkTheme = false,
            };
            var createResponse = await _client.PostAsJsonAsync("/api/ProblemBox", creationDTO);
            var created = await createResponse.Content.ReadFromJsonAsync<ProblemBoxCreatedDTO>();

            var response = await _client.DeleteAsync($"/api/ProblemBox/{created.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteProblemBox_ReturnsInternalServerError_WhenNotFound()
        {
            var response = await _client.DeleteAsync($"/api/ProblemBox/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }
}
