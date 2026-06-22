using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using ProblemService.Clients;
using ProblemService.Context;
using ProblemService.Enums;
using ProblemService.Models.DTOs;
using Xunit;

namespace ProblemServiceIntegrationTests
{
    // Test stub: always reports the box as active and public so create flows succeed.
    public class ActiveProblemBoxServiceClient : ProblemBoxServiceClient
    {
        public override Task<bool> IsBoxActiveAsync(Guid boxId, string? bearerHeader, CancellationToken requestCt)
            => Task.FromResult(true);

        public override Task<bool> HasPasswordAsync(Guid boxId, string? bearerHeader, CancellationToken requestCt)
            => Task.FromResult(false);
    }

    public class ProblemIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;

        public ProblemIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Remove real SQL Server DbContext
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ProblemContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Add InMemory database
                    services.AddDbContext<ProblemContext>((options) =>
                    {
                        options.UseInMemoryDatabase("TestProblemDb");
                    });

                    // Remove HttpClients for external services
                    var attachmentDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IHttpClientFactory));
                    if (attachmentDescriptor != null)
                        services.Remove(attachmentDescriptor);

                    // Replace the box-status gate with a stub that always allows submissions
                    var boxClientDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(ProblemBoxServiceClient));
                    if (boxClientDescriptor != null)
                        services.Remove(boxClientDescriptor);
                    services.AddScoped<ProblemBoxServiceClient, ActiveProblemBoxServiceClient>();
                });
            });

            _client = _factory.CreateClient();
        }

        // GET ALL PROBLEMS
        [Fact]
        public async Task GetAllProblems_ReturnsOkResponse()
        {
            var response = await _client.GetAsync("/api/Problem");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAllProblems_ReturnsEmptyList_WhenNoProblemExists()
        {
            var response = await _client.GetAsync("/api/Problem");
            var result = await response.Content.ReadFromJsonAsync<List<ProblemDTO>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
        }

        // CREATE PROBLEM
        [Fact]
        public async Task CreateProblem_ReturnsCreated_WhenValidData()
        {
            var creationDTO = new ProblemCreationDTO
            {
                Title = "Integration Test Problem",
                Description = "Integration Test Description",
                ProblemBoxId = Guid.NewGuid(),
                Status = ProblemSuggestionStatus.Active,
                Priority = ProblemPriority.High
            };

            var response = await _client.PostAsJsonAsync("/api/Problem", creationDTO);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

       

      

        
        // GET BY ID
        [Fact]
        public async Task GetProblemById_ReturnsOk_WhenProblemExists()
        {
            var creationDTO = new ProblemCreationDTO
            {
                Title = "Integration Test Problem",
                Description = "Integration Test Description",
                ProblemBoxId = Guid.NewGuid(),
                Status = ProblemSuggestionStatus.Active,
                Priority = ProblemPriority.High
            };
            var createResponse = await _client.PostAsJsonAsync("/api/Problem", creationDTO);
            var created = await createResponse.Content.ReadFromJsonAsync<ProblemCreatedDTO>();

            var response = await _client.GetAsync($"/api/Problem/{created.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetProblemById_ReturnsInternalServerError_WhenProblemNotFound()
        {
            var response = await _client.GetAsync($"/api/Problem/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // GET BY PROBLEMBOX ID
        [Fact]
        public async Task GetProblemsByProblemBoxId_ReturnsOk_WhenProblemsExist()
        {
            var problemBoxId = Guid.NewGuid();
            var creationDTO = new ProblemCreationDTO
            {
                Title = "Integration Test Problem",
                Description = "Integration Test Description",
                ProblemBoxId = problemBoxId,
                Status = ProblemSuggestionStatus.Active,
                Priority = ProblemPriority.High
            };
            await _client.PostAsJsonAsync("/api/Problem", creationDTO);

            var response = await _client.GetAsync($"/api/Problem/problembox/{problemBoxId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetProblemsByProblemBoxId_ReturnsOk_WithEmptyList()
        {
            var response = await _client.GetAsync($"/api/Problem/problembox/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // UPDATE PROBLEM
        [Fact]
        public async Task UpdateProblem_ReturnsOk_WhenProblemExists()
        {
            var creationDTO = new ProblemCreationDTO
            {
                Title = "Integration Test Problem",
                Description = "Integration Test Description",
                ProblemBoxId = Guid.NewGuid(),
                Status = ProblemSuggestionStatus.Active,
                Priority = ProblemPriority.High
            };
            var createResponse = await _client.PostAsJsonAsync("/api/Problem", creationDTO);
            var created = await createResponse.Content.ReadFromJsonAsync<ProblemCreatedDTO>();

            var updateDTO = new ProblemUpdateDTO
            {
                Id = created.Id,
                Title = "Updated Problem",
                Description = "Updated Description",
                ProblemBoxId = Guid.NewGuid(),
                Status = ProblemSuggestionStatus.Active,
                Priority = ProblemPriority.High
            };

            var response = await _client.PutAsJsonAsync("/api/Problem", updateDTO);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProblem_ReturnsInternalServerError_WhenProblemNotFound()
        {
            var updateDTO = new ProblemUpdateDTO
            {
                Id = Guid.NewGuid(),
                Title = "Updated Problem",
                Description = "Updated Description",
                ProblemBoxId = Guid.NewGuid(),
                Status = ProblemSuggestionStatus.Active,
                Priority = ProblemPriority.High
            };

            var response = await _client.PutAsJsonAsync("/api/Problem", updateDTO);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // DELETE PROBLEM
        [Fact]
        public async Task DeleteProblem_ReturnsNoContent_WhenProblemExists()
        {
            var creationDTO = new ProblemCreationDTO
            {
                Title = "Integration Test Problem",
                Description = "Integration Test Description",
                ProblemBoxId = Guid.NewGuid(),
                Status = ProblemSuggestionStatus.Active,
                Priority = ProblemPriority.High
            };
            var createResponse = await _client.PostAsJsonAsync("/api/Problem", creationDTO);
            var created = await createResponse.Content.ReadFromJsonAsync<ProblemCreatedDTO>();

            var response = await _client.DeleteAsync($"/api/Problem/{created.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteProblem_ReturnsInternalServerError_WhenProblemNotFound()
        {
            var response = await _client.DeleteAsync($"/api/Problem/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }
}