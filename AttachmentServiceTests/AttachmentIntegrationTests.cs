using System.Net;
using System.Net.Http.Json;
using AnonymousDomain.Models.Attachment;
using AttachmentService.Context;
using AttachmentService.Models.Attachment.DTOs;
using AttachmentService.Models.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Xunit;

namespace AttachmentServiceTests.Integration
{
    public class AttachmentServiceWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "ConnectionStrings:AttachmentDB", "Server=fake;Database=fake;" }
                });
            });

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AttachmentContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                var dbDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(AttachmentContext));
                if (dbDescriptor != null)
                    services.Remove(dbDescriptor);

                services.AddDbContext<AttachmentContext>(options =>
                    options.UseInMemoryDatabase("AttachmentIntegrationTestDb"));
            });

            builder.UseEnvironment("Testing");
        }
    }

    public class AttachmentIntegrationTests : IClassFixture<AttachmentServiceWebAppFactory>
    {
        private readonly HttpClient _client;
        private readonly AttachmentServiceWebAppFactory _factory;

        public AttachmentIntegrationTests(AttachmentServiceWebAppFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private Guid SeedAttachment(Guid? suggestionId = null, Guid? problemId = null)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AttachmentContext>();
            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                FileName = "test.pdf",
                FileType = "pdf",
                Url = "http://test.com/test.pdf",
                UploadedAt = DateTime.UtcNow,
                SuggestionId = suggestionId ?? Guid.NewGuid(),
                ProblemId = problemId
            };
            context.Attachments.Add(attachment);
            context.SaveChanges();
            return attachment.Id;
        }

        [Fact]
        public async Task GetAllAttachments_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/Attachment");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAllAttachments_ReturnsJsonContentType()
        {
            var response = await _client.GetAsync("/api/Attachment");
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task GetAllAttachments_ReturnsList()
        {
            var response = await _client.GetAsync("/api/Attachment");
            var returned = await response.Content.ReadFromJsonAsync<IEnumerable<AttachmentDTO>>();
            Assert.NotNull(returned);
        }

        [Fact]
        public async Task GetAttachmentBySuggestionId_ReturnsOk()
        {
            var suggestionId = Guid.NewGuid();
            SeedAttachment(suggestionId: suggestionId);

            var response = await _client.GetAsync($"/api/Attachment/suggestion/{suggestionId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAttachmentBySuggestionId_ReturnsCorrectAttachments()
        {
            var suggestionId = Guid.NewGuid();
            SeedAttachment(suggestionId: suggestionId);

            var response = await _client.GetAsync($"/api/Attachment/suggestion/{suggestionId}");
            var returned = await response.Content.ReadFromJsonAsync<IEnumerable<AttachmentDTO>>();
            Assert.NotNull(returned);
            Assert.Single(returned!);
        }

        [Fact]
        public async Task GetAttachmentBySuggestionId_ReturnsEmpty_WhenNoneExist()
        {
            var response = await _client.GetAsync($"/api/Attachment/suggestion/{Guid.NewGuid()}");
            var returned = await response.Content.ReadFromJsonAsync<IEnumerable<AttachmentDTO>>();
            Assert.NotNull(returned);
            Assert.Empty(returned!);
        }

        [Fact]
        public async Task GetAttachmentByProblemId_ReturnsOk()
        {
            var problemId = Guid.NewGuid();
            SeedAttachment(problemId: problemId);

            var response = await _client.GetAsync($"/api/Attachment/problem/{problemId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAttachmentByProblemId_ReturnsCorrectAttachments()
        {
            var problemId = Guid.NewGuid();
            SeedAttachment(problemId: problemId);

            var response = await _client.GetAsync($"/api/Attachment/problem/{problemId}");
            var returned = await response.Content.ReadFromJsonAsync<IEnumerable<AttachmentDTO>>();
            Assert.NotNull(returned);
            Assert.Single(returned!);
        }

        [Fact]
        public async Task GetAttachmentByProblemId_ReturnsEmpty_WhenNoneExist()
        {
            var response = await _client.GetAsync($"/api/Attachment/problem/{Guid.NewGuid()}");
            var returned = await response.Content.ReadFromJsonAsync<IEnumerable<AttachmentDTO>>();
            Assert.NotNull(returned);
            Assert.Empty(returned!);
        }

        [Fact]
        public async Task CreateAttachment_ReturnsCreated()
        {
            var creationDto = new AttachmentCreationDTO
            {
                FileName = "new.pdf",
                FileType = "pdf",
                Url = "http://test.com/new.pdf",
                SuggestionId = Guid.NewGuid()
            };

            var response = await _client.PostAsJsonAsync("/api/Attachment", creationDto);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateAttachment_ReturnsCorrectData()
        {
            var creationDto = new AttachmentCreationDTO
            {
                FileName = "new.pdf",
                FileType = "pdf",
                Url = "http://test.com/new.pdf",
                SuggestionId = Guid.NewGuid()
            };

            var response = await _client.PostAsJsonAsync("/api/Attachment", creationDto);
            var returned = await response.Content.ReadFromJsonAsync<AttachmentDTO>();
            Assert.NotNull(returned);
            Assert.Equal("new.pdf", returned!.FileName);
        }

        [Fact]
        public async Task UpdateAttachment_ReturnsOk()
        {
            var id = SeedAttachment();
            var updateDto = new AttachmentUpdateDTO
            {
                Id = id,
                FileName = "updated.pdf",
                FileType = "pdf",
                Url = "http://test.com/updated.pdf"
            };

            var response = await _client.PutAsJsonAsync("/api/Attachment", updateDto);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateAttachment_ReturnsUpdatedData()
        {
            var id = SeedAttachment();
            var updateDto = new AttachmentUpdateDTO
            {
                Id = id,
                FileName = "updated.pdf",
                FileType = "pdf",
                Url = "http://test.com/updated.pdf"
            };

            var response = await _client.PutAsJsonAsync("/api/Attachment", updateDto);
            var returned = await response.Content.ReadFromJsonAsync<AttachmentDTO>();
            Assert.NotNull(returned);
            Assert.Equal("updated.pdf", returned!.FileName);
        }

        [Fact]
        public async Task DeleteAttachment_ReturnsNoContent()
        {
            var id = SeedAttachment();
            var response = await _client.DeleteAsync($"/api/Attachment/{id}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteAttachment_ReturnsNoContent_WhenNotFound()
        {
            var response = await _client.DeleteAsync($"/api/Attachment/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
