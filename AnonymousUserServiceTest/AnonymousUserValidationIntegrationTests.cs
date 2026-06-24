using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AnonymousUserService.Clients;
using AnonymousUserService.Context;
using AnonymousUserService.Models.DTOs.AnonymousUser;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace AnonymousUserService.Tests.Integration
{
    // ───────────────────────────────────────────────────────────────────────────
    // task 003: prove the BACKEND is the real validation gate for
    // POST /api/AnonymousUser (the anonymous register endpoint, not [Authorize]).
    //
    // This also guards the task-002 controller fix: ArgumentException thrown by the
    // repository (password policy / breach / username) must surface as
    // 400 { "error": "<message>" }  — previously this leaked as 500 / { message }.
    //
    // The real IPwnedPasswordsClient is replaced with a deterministic fake so the
    // breach gate is exercised without network access.
    // ───────────────────────────────────────────────────────────────────────────

    public class FakePwnedPasswordsClient : IPwnedPasswordsClient
    {
        public const string BreachedPassword = "Breach3d$Pass1";

        public Task<bool> IsBreachedAsync(string password, CancellationToken ct)
            => Task.FromResult(password == BreachedPassword);
    }

    // Captcha always verifies so the username/password gates are the ones under test
    // (the real client is fail-closed and would reject every request without a token).
    public class FakeCaptchaVerifierClient : ICaptchaVerifierClient
    {
        public Task<bool> VerifyAsync(string? token, CancellationToken ct)
            => Task.FromResult(true);
    }

    public class AnonymousUserValidationWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AnonymousUserContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<AnonymousUserContext>(options =>
                    options.UseInMemoryDatabase("AnonymousUserValidationIntegrationTestDb"));
            });

            builder.UseEnvironment("Testing");
        }
    }

    public class AnonymousUserValidationIntegrationTests : IClassFixture<AnonymousUserValidationWebAppFactory>
    {
        private readonly HttpClient _client;

        public AnonymousUserValidationIntegrationTests(AnonymousUserValidationWebAppFactory factory)
        {
            var customized = factory.WithWebHostBuilder(b =>
                b.ConfigureTestServices(s =>
                {
                    s.RemoveAll<IPwnedPasswordsClient>();
                    s.AddScoped<IPwnedPasswordsClient>(_ => new FakePwnedPasswordsClient());
                    s.RemoveAll<ICaptchaVerifierClient>();
                    s.AddScoped<ICaptchaVerifierClient>(_ => new FakeCaptchaVerifierClient());
                }));

            _client = customized.CreateClient();
        }

        // Assert the body is exactly { "error": "<expected>" } — guards the task-002
        // controller fix: 'error' present with the expected value, 'message' absent.
        private static async Task AssertErrorBody(HttpResponseMessage response, string expectedMessage)
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("error", out var error),
                $"Response body had no 'error' property. Body: {json}");
            Assert.Equal(expectedMessage, error.GetString());
            Assert.False(root.TryGetProperty("message", out _),
                $"Response body unexpectedly contained a 'message' property. Body: {json}");
        }

        [Fact]
        public async Task Create_BadUsername_Returns400_WithUsernameMessage()
        {
            var dto = new AnonymousUserCreationDTO
            {
                Username = "bad name!",       // space + '!' -> invalid
                Password = "Zx9$mQ2!vK7w"     // strong, so password is not the failing field
            };

            var response = await _client.PostAsJsonAsync("/api/AnonymousUser", dto);

            // The backend repository is now the single validation gate: the bad
            // username is rejected at the HTTP boundary with the canonical message
            // in the strict { "error": "<message>" } shape (no { errors }/{ message }).
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await AssertErrorBody(response,
                "Username may only contain letters, digits, and underscores (3–30 chars).");
        }

        [Fact]
        public async Task Create_EmptyUsername_Returns400_WithUsernameMessage_AndErrorShape()
        {
            var dto = new AnonymousUserCreationDTO
            {
                Username = "",                // empty -> repo username guard fires
                Password = "Zx9$mQ2!vK7w"
            };

            var response = await _client.PostAsJsonAsync("/api/AnonymousUser", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await AssertErrorBody(response,
                "Username may only contain letters, digits, and underscores (3–30 chars).");
        }

        [Fact]
        public async Task Create_EmptyPassword_Returns400_WithTooShortMessage_AndErrorShape()
        {
            var dto = new AnonymousUserCreationDTO
            {
                Username = "validuser_pw",
                Password = ""                 // empty -> password too-short guard fires
            };

            var response = await _client.PostAsJsonAsync("/api/AnonymousUser", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await AssertErrorBody(response, "Password must be at least 8 characters.");
        }

        [Fact]
        public async Task Create_WeakPassword_Returns400_WithComplexityMessage_AndErrorShape()
        {
            var dto = new AnonymousUserCreationDTO
            {
                Username = "validuser_int",
                Password = "abcdefgh"  // 8 lowercase: passes DTO length, fails repo complexity
            };

            var response = await _client.PostAsJsonAsync("/api/AnonymousUser", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await AssertErrorBody(response,
                "Password must include uppercase, lowercase, a number, and a special character.");
        }

        [Fact]
        public async Task Create_BreachedPassword_Returns400_WithBreachMessage_AndErrorShape()
        {
            var dto = new AnonymousUserCreationDTO
            {
                Username = "breached_int",
                Password = FakePwnedPasswordsClient.BreachedPassword
            };

            var response = await _client.PostAsJsonAsync("/api/AnonymousUser", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await AssertErrorBody(response,
                "This password has appeared in a data breach. Please choose another.");
        }

        [Fact]
        public async Task Create_ValidUsernameAndStrongPassword_Returns201()
        {
            var dto = new AnonymousUserCreationDTO
            {
                Username = $"u{Guid.NewGuid():N}".Substring(0, 20),
                Password = "Zx9$mQ2!vK7w"
            };

            var response = await _client.PostAsJsonAsync("/api/AnonymousUser", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<AnonymousUserDTO>();
            Assert.NotNull(created);
            Assert.NotEqual(Guid.Empty, created!.Id);
        }
    }
}
