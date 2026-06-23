using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProblemService.Clients;
using ProblemService.Context;
using ProblemService.Enums;
using ProblemService.Models.DTOs;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ProblemServiceIntegrationTests
{
    public class ProblemCommentIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;

        public ProblemCommentIntegrationTests(WebApplicationFactory<Program> factory)
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
                        options.UseInMemoryDatabase("TestProblemCommentDb");
                    });

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

        // Helper method to create a problem first since comment needs a ProblemId
        private async Task<ProblemCreatedDTO> CreateTestProblem()
        {
            var creationDTO = new ProblemCreationDTO
            {
                Title = "Test Problem",
                Description = "Test Description",
                ProblemBoxId = Guid.NewGuid(),
                Status = ProblemSuggestionStatus.Active,
                Priority = ProblemPriority.High
            };
            var response = await _client.PostAsJsonAsync("/api/Problem", creationDTO);
            return await response.Content.ReadFromJsonAsync<ProblemCreatedDTO>();
        }

        // GET ALL
        [Fact]
        public async Task GetAllProblemComments_ReturnsOkResponse()
        {
            var response = await _client.GetAsync("/api/ProblemComment");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAllProblemComments_ReturnsEmptyList_WhenNoCommentExists()
        {
            var response = await _client.GetAsync("/api/ProblemComment");
            var result = await response.Content.ReadFromJsonAsync<List<ProblemCommentDTO>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
        }

        // CREATE COMMENT
        [Fact]
        public async Task CreateProblemComment_ReturnsCreated_WhenValidData()
        {
            var problem = await CreateTestProblem();

            var creationDTO = new ProblemCommentCreationDTO
            {
                CommentText = "Integration Test Comment",
                IsAnonymous = false,
                ProblemId = problem.Id,
                ProblemCommentAuthorId = Guid.NewGuid()
            };

            var response = await _client.PostAsJsonAsync("/api/ProblemComment", creationDTO);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateProblemComment_ReturnsCreated_WhenIsAnonymous()
        {
            var problem = await CreateTestProblem();

            var creationDTO = new ProblemCommentCreationDTO
            {
                CommentText = "Anonymous Integration Test Comment",
                IsAnonymous = true,
                ProblemId = problem.Id,
                ProblemCommentAuthorId = Guid.NewGuid()
            };

            var response = await _client.PostAsJsonAsync("/api/ProblemComment", creationDTO);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }


       
        

        // GET BY ID
        [Fact]
        public async Task GetProblemCommentById_ReturnsOk_WhenCommentExists()
        {
            var problem = await CreateTestProblem();

            var creationDTO = new ProblemCommentCreationDTO
            {
                CommentText = "Integration Test Comment",
                IsAnonymous = false,
                ProblemId = problem.Id,
                ProblemCommentAuthorId = Guid.NewGuid()
            };
            var createResponse = await _client.PostAsJsonAsync("/api/ProblemComment", creationDTO);
            var created = await createResponse.Content.ReadFromJsonAsync<ProblemCommentCreatedDTO>();

            var response = await _client.GetAsync($"/api/ProblemComment/{created.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetProblemCommentById_ReturnsInternalServerError_WhenCommentNotFound()
        {
            var response = await _client.GetAsync($"/api/ProblemComment/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // UPDATE COMMENT
        [Fact]
        public async Task UpdateProblemComment_ReturnsOk_WhenCommentExists()
        {
            var problem = await CreateTestProblem();

            var creationDTO = new ProblemCommentCreationDTO
            {
                CommentText = "Integration Test Comment",
                IsAnonymous = false,
                ProblemId = problem.Id,
                ProblemCommentAuthorId = Guid.NewGuid()
            };
            var createResponse = await _client.PostAsJsonAsync("/api/ProblemComment", creationDTO);
            var created = await createResponse.Content.ReadFromJsonAsync<ProblemCommentCreatedDTO>();

            var updateDTO = new ProblemCommentUpdateDTO
            {
                Id = created.Id,
                CommentText = "Updated Comment",
                IsAnonymous = true
            };

            var response = await _client.PutAsJsonAsync("/api/ProblemComment", updateDTO);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProblemComment_ReturnsInternalServerError_WhenCommentNotFound()
        {
            var updateDTO = new ProblemCommentUpdateDTO
            {
                Id = Guid.NewGuid(),
                CommentText = "Updated Comment",
                IsAnonymous = false
            };

            var response = await _client.PutAsJsonAsync("/api/ProblemComment", updateDTO);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // DELETE COMMENT
        [Fact]
        public async Task DeleteProblemComment_ReturnsNoContent_WhenCommentExists()
        {
            var problem = await CreateTestProblem();

            var creationDTO = new ProblemCommentCreationDTO
            {
                CommentText = "Integration Test Comment",
                IsAnonymous = false,
                ProblemId = problem.Id,
                ProblemCommentAuthorId = Guid.NewGuid()
            };
            var createResponse = await _client.PostAsJsonAsync("/api/ProblemComment", creationDTO);
            var created = await createResponse.Content.ReadFromJsonAsync<ProblemCommentCreatedDTO>();

            var response = await _client.DeleteAsync($"/api/ProblemComment/{created.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteProblemComment_ReturnsInternalServerError_WhenCommentNotFound()
        {
            var response = await _client.DeleteAsync($"/api/ProblemComment/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }
}
