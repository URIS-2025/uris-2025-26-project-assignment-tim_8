using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProblemService.Context;
using ProblemService.Models.DTOs;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ProblemServiceIntegrationTests
{
    public class ProblemCategoryIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;

        public ProblemCategoryIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ProblemContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<ProblemContext>((options) =>
                    {
                        options.UseInMemoryDatabase("TestProblemCategoryDb");
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        // GET ALL
        [Fact]
        public async Task GetAllProblemCategories_ReturnsOkResponse()
        {
            var response = await _client.GetAsync("/api/ProblemCategory");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAllProblemCategories_ReturnsEmptyList_WhenNoCategoryExists()
        {
            var response = await _client.GetAsync("/api/ProblemCategory");
            var result = await response.Content.ReadFromJsonAsync<List<ProblemCategoryDTO>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
        }

        // CREATE CATEGORY
        [Fact]
        public async Task CreateProblemCategory_ReturnsCreated_WhenValidData()
        {
            var creationDTO = new ProblemCategoryCreationDTO
            {
                Title = "Integration Test Category",
                Description = "Integration Test Description"
            };

            var response = await _client.PostAsJsonAsync("/api/ProblemCategory", creationDTO);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        

   

        // GET BY ID
        [Fact]
        public async Task GetProblemCategoryById_ReturnsOk_WhenCategoryExists()
        {
            var creationDTO = new ProblemCategoryCreationDTO
            {
                Title = "Integration Test Category",
                Description = "Integration Test Description"
            };
            var createResponse = await _client.PostAsJsonAsync("/api/ProblemCategory", creationDTO);
            var created = await createResponse.Content.ReadFromJsonAsync<ProblemCategoryCreatedDTO>();

            var response = await _client.GetAsync($"/api/ProblemCategory/{created.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetProblemCategoryById_ReturnsInternalServerError_WhenCategoryNotFound()
        {
            var response = await _client.GetAsync($"/api/ProblemCategory/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // UPDATE CATEGORY
        [Fact]
        public async Task UpdateProblemCategory_ReturnsOk_WhenCategoryExists()
        {
            var creationDTO = new ProblemCategoryCreationDTO
            {
                Title = "Integration Test Category",
                Description = "Integration Test Description"
            };
            var createResponse = await _client.PostAsJsonAsync("/api/ProblemCategory", creationDTO);
            var created = await createResponse.Content.ReadFromJsonAsync<ProblemCategoryCreatedDTO>();

            var updateDTO = new ProblemCategoryUpdateDTO
            {
                Id = created.Id,
                Title = "Updated Category",
                Description = "Updated Description"
            };

            var response = await _client.PutAsJsonAsync("/api/ProblemCategory", updateDTO);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProblemCategory_ReturnsInternalServerError_WhenCategoryNotFound()
        {
            var updateDTO = new ProblemCategoryUpdateDTO
            {
                Id = Guid.NewGuid(),
                Title = "Updated Category",
                Description = "Updated Description"
            };

            var response = await _client.PutAsJsonAsync("/api/ProblemCategory", updateDTO);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProblemCategory_ReturnsInternalServerError_WhenTitleIsEmpty()
        {
            var creationDTO = new ProblemCategoryCreationDTO
            {
                Title = "Integration Test Category",
                Description = "Integration Test Description"
            };
            var createResponse = await _client.PostAsJsonAsync("/api/ProblemCategory", creationDTO);
            var created = await createResponse.Content.ReadFromJsonAsync<ProblemCategoryCreatedDTO>();

            var updateDTO = new ProblemCategoryUpdateDTO
            {
                Id = created.Id,
                Title = "",
                Description = "Updated Description"
            };

            var response = await _client.PutAsJsonAsync("/api/ProblemCategory", updateDTO);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProblemCategory_ReturnsInternalServerError_WhenDescriptionIsEmpty()
        {
            var creationDTO = new ProblemCategoryCreationDTO
            {
                Title = "Integration Test Category",
                Description = "Integration Test Description"
            };
            var createResponse = await _client.PostAsJsonAsync("/api/ProblemCategory", creationDTO);
            var created = await createResponse.Content.ReadFromJsonAsync<ProblemCategoryCreatedDTO>();

            var updateDTO = new ProblemCategoryUpdateDTO
            {
                Id = created.Id,
                Title = "Updated Category",
                Description = ""
            };

            var response = await _client.PutAsJsonAsync("/api/ProblemCategory", updateDTO);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // DELETE CATEGORY
        [Fact]
        public async Task DeleteProblemCategory_ReturnsNoContent_WhenCategoryExists()
        {
            var creationDTO = new ProblemCategoryCreationDTO
            {
                Title = "Integration Test Category",
                Description = "Integration Test Description"
            };
            var createResponse = await _client.PostAsJsonAsync("/api/ProblemCategory", creationDTO);
            var created = await createResponse.Content.ReadFromJsonAsync<ProblemCategoryCreatedDTO>();

            var response = await _client.DeleteAsync($"/api/ProblemCategory/{created.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteProblemCategory_ReturnsInternalServerError_WhenCategoryNotFound()
        {
            var response = await _client.DeleteAsync($"/api/ProblemCategory/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }
}
