using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AnonymousDomain.Models.Organization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using OrganizationService.Clients;
using OrganizationService.Context;
using OrganizationService.Models.DTOs;
using Xunit;
using OrganizationService;

namespace OrganizationService.Tests.Integration
{
    // Fail-closed captcha is bypassed in these integration tests: always reports the
    // token as verified so CreateUser/Login flows run without a real Turnstile token.
    public class AlwaysPassCaptchaVerifierClient : ICaptchaVerifierClient
    {
        public Task<bool> VerifyAsync(string? token, CancellationToken ct) => Task.FromResult(true);
    }

    public class OrganizationServiceWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Ukloni postojeći DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<OrganizationContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Dodaj InMemory bazu
                services.AddDbContext<OrganizationContext>(options =>
                    options.UseInMemoryDatabase("OrganizationIntegrationTestDb"));

                // Fail-closed captcha would reject CreateUser without a real token;
                // swap in a stub that always verifies so the user flow is exercised.
                services.RemoveAll<ICaptchaVerifierClient>();
                services.AddScoped<ICaptchaVerifierClient, AlwaysPassCaptchaVerifierClient>();
            });

            builder.UseEnvironment("Testing");
        }
    }

    // ─── ORGANIZATION ENDPOINTS ───────────────────────────────────────────────

    public class OrganizationIntegrationTests : IClassFixture<OrganizationServiceWebAppFactory>
    {
        private readonly HttpClient _client;

        public OrganizationIntegrationTests(OrganizationServiceWebAppFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAllOrganizations_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/Organization");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAllOrganizations_ReturnsJsonContentType()
        {
            var response = await _client.GetAsync("/api/Organization");

            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task CreateOrganization_ReturnsCreated()
        {
            var dto = new OrganizationCreationDTO
            {
                Name    = "Integraciona Org",
                AdminId = Guid.NewGuid()
            };

            var response = await _client.PostAsJsonAsync("/api/Organization", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrganization_ReturnsCreatedWithCorrectName()
        {
            var dto = new OrganizationCreationDTO
            {
                Name    = "Test Org Integration",
                AdminId = Guid.NewGuid()
            };

            var response = await _client.PostAsJsonAsync("/api/Organization", dto);
            var returned = await response.Content.ReadFromJsonAsync<OrganizationCreatedDTO>();

            Assert.Equal("Test Org Integration", returned!.Name);
            Assert.NotEqual(Guid.Empty, returned.Id);
        }

        [Fact]
        public async Task GetOrganizationById_ReturnsOk_WhenFound()
        {
            // Prvo kreiraj
            var createDto = new OrganizationCreationDTO { Name = "Get By Id Org", AdminId = Guid.NewGuid() };
            var createResponse = await _client.PostAsJsonAsync("/api/Organization", createDto);
            var created = await createResponse.Content.ReadFromJsonAsync<OrganizationCreatedDTO>();

            // Pa dohvati
            var response = await _client.GetAsync($"/api/Organization/{created!.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetOrganizationById_ReturnsCorrectOrganization()
        {
            var createDto = new OrganizationCreationDTO { Name = "Specific Org", AdminId = Guid.NewGuid() };
            var createResponse = await _client.PostAsJsonAsync("/api/Organization", createDto);
            var created = await createResponse.Content.ReadFromJsonAsync<OrganizationCreatedDTO>();

            var response = await _client.GetAsync($"/api/Organization/{created!.Id}");
            var returned = await response.Content.ReadFromJsonAsync<OrganizationDTO>();

            Assert.Equal(created.Id, returned!.Id);
            Assert.Equal("Specific Org", returned.Name);
        }

        [Fact]
        public async Task UpdateOrganization_ReturnsOk()
        {
            var createDto = new OrganizationCreationDTO { Name = "Old Name", AdminId = Guid.NewGuid() };
            var createResponse = await _client.PostAsJsonAsync("/api/Organization", createDto);
            var created = await createResponse.Content.ReadFromJsonAsync<OrganizationCreatedDTO>();

            var updateDto = new OrganizationDTO { Id = created!.Id, Name = "New Name", AdminId = Guid.NewGuid() };
            var response = await _client.PutAsJsonAsync("/api/Organization", updateDto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrganization_UpdatesName()
        {
            var adminId   = Guid.NewGuid();
            var createDto = new OrganizationCreationDTO { Name = "Before Update", AdminId = adminId };
            var createResponse = await _client.PostAsJsonAsync("/api/Organization", createDto);
            var created = await createResponse.Content.ReadFromJsonAsync<OrganizationCreatedDTO>();

            var updateDto = new OrganizationDTO { Id = created!.Id, Name = "After Update", AdminId = adminId };
            await _client.PutAsJsonAsync("/api/Organization", updateDto);

            var getResponse = await _client.GetAsync($"/api/Organization/{created.Id}");
            var returned = await getResponse.Content.ReadFromJsonAsync<OrganizationDTO>();

            Assert.Equal("After Update", returned!.Name);
        }

        [Fact]
        public async Task DeleteOrganization_ReturnsNoContent()
        {
            var createDto = new OrganizationCreationDTO { Name = "To Delete", AdminId = Guid.NewGuid() };
            var createResponse = await _client.PostAsJsonAsync("/api/Organization", createDto);
            var created = await createResponse.Content.ReadFromJsonAsync<OrganizationCreatedDTO>();

            var response = await _client.DeleteAsync($"/api/Organization/{created!.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    // ─── USER ENDPOINTS ───────────────────────────────────────────────────────

    public class UserIntegrationTests : IClassFixture<OrganizationServiceWebAppFactory>
    {
        private readonly HttpClient _client;

        public UserIntegrationTests(OrganizationServiceWebAppFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAllUsers_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/User");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateUser_ReturnsCreated()
        {
            var dto = new UserCreationDTO
            {
                Name           = "Marko",
                Surname        = "Markovic",
                Email          = "marko@integration.com",
                Password       = "Zx9$mQ2!vK7w",
                Username       = "markom_int",
                RoleId         = Guid.NewGuid(),
                OrganizationId = null
            };

            var response = await _client.PostAsJsonAsync("/api/User", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateUser_ReturnsCorrectUsernameAndEmail()
        {
            var dto = new UserCreationDTO
            {
                Name     = "Ana",
                Surname  = "Anic",
                Email    = "ana@integration.com",
                Password = "Zx9$mQ2!vK7w",
                Username = "anaa_int",
                RoleId   = Guid.NewGuid()
            };

            var response = await _client.PostAsJsonAsync("/api/User", dto);
            var returned = await response.Content.ReadFromJsonAsync<UserCreatedDTO>();

            Assert.Equal("anaa_int", returned!.Username);
            Assert.Equal("ana@integration.com", returned.Email);
        }

        [Fact]
        public async Task GetUserById_ReturnsOk_WhenFound()
        {
            var createDto = new UserCreationDTO
            {
                Name     = "Test",
                Surname  = "Testic",
                Email    = "test@integration.com",
                Password = "sifra123",
                Username = "testuser_int",
                RoleId   = Guid.NewGuid()
            };
            var createResponse = await _client.PostAsJsonAsync("/api/User", createDto);
            var created = await createResponse.Content.ReadFromJsonAsync<UserCreatedDTO>();

            var response = await _client.GetAsync($"/api/User/{created!.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DeleteUser_ReturnsNoContent()
        {
            var createDto = new UserCreationDTO
            {
                Name     = "Delete",
                Surname  = "Me",
                Email    = "delete@integration.com",
                Password = "sifra123",
                Username = "deleteme_int",
                RoleId   = Guid.NewGuid()
            };
            var createResponse = await _client.PostAsJsonAsync("/api/User", createDto);
            var created = await createResponse.Content.ReadFromJsonAsync<UserCreatedDTO>();

            var response = await _client.DeleteAsync($"/api/User/{created!.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    // ─── USER ROLE ENDPOINTS ──────────────────────────────────────────────────

    public class UserRoleIntegrationTests : IClassFixture<OrganizationServiceWebAppFactory>
    {
        private readonly HttpClient _client;

        public UserRoleIntegrationTests(OrganizationServiceWebAppFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAllUserRoles_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/UserRole");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateUserRole_ReturnsCreated()
        {
            var dto = new UserRoleCreationDTO { Title = "Integration Role", Description = "Opis" };

            var response = await _client.PostAsJsonAsync("/api/UserRole", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateUserRole_ReturnsCorrectTitle()
        {
            var dto = new UserRoleCreationDTO { Title = "Admin Integration", Description = "Puni pristup" };

            var response = await _client.PostAsJsonAsync("/api/UserRole", dto);
            var returned = await response.Content.ReadFromJsonAsync<UserRoleCreatedDTO>();

            Assert.Equal("Admin Integration", returned!.Title);
            Assert.NotEqual(Guid.Empty, returned.Id);
        }

        [Fact]
        public async Task GetUserRoleById_ReturnsOk_WhenFound()
        {
            var createDto = new UserRoleCreationDTO { Title = "Get By Id Role", Description = "Opis" };
            var createResponse = await _client.PostAsJsonAsync("/api/UserRole", createDto);
            var created = await createResponse.Content.ReadFromJsonAsync<UserRoleCreatedDTO>();

            var response = await _client.GetAsync($"/api/UserRole/{created!.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DeleteUserRole_ReturnsNoContent()
        {
            var createDto = new UserRoleCreationDTO { Title = "To Delete Role", Description = "Opis" };
            var createResponse = await _client.PostAsJsonAsync("/api/UserRole", createDto);
            var created = await createResponse.Content.ReadFromJsonAsync<UserRoleCreatedDTO>();

            var response = await _client.DeleteAsync($"/api/UserRole/{created!.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
