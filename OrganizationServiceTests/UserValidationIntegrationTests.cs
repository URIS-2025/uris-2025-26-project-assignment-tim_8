using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrganizationService.Clients;
using OrganizationService.Context;
using OrganizationService.Models.DTOs;
using Xunit;

namespace OrganizationService.Tests.Integration
{
    // ───────────────────────────────────────────────────────────────────────────
    // task 003: prove the BACKEND is the real validation gate for POST /api/User.
    //
    // The endpoint is anonymous (not [Authorize]) and must reject bad input at the
    // HTTP boundary with the canonical shared-contract messages, in the error shape
    // 400 { "error": "<message>" }  (NOT { "message": ... }).
    //
    // The real IPwnedPasswordsClient is swapped for a deterministic fake so the
    // breach gate is exercised without network access. A designated password is
    // reported as breached; everything else is not.
    // ───────────────────────────────────────────────────────────────────────────

    public class FakePwnedPasswordsClient : IPwnedPasswordsClient
    {
        public const string BreachedPassword = "Breach3d$Pass1";

        public Task<bool> IsBreachedAsync(string password, CancellationToken ct)
            => Task.FromResult(password == BreachedPassword);
    }

    // Fail-closed captcha is bypassed in tests: always reports the token as verified
    // so the validation flow (email/password/breach) is exercised, not the captcha gate.
    public class FakeCaptchaVerifierClient : ICaptchaVerifierClient
    {
        public Task<bool> VerifyAsync(string? token, CancellationToken ct) => Task.FromResult(true);
    }

    public class UserValidationWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<OrganizationContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<OrganizationContext>(options =>
                    options.UseInMemoryDatabase("OrganizationValidationIntegrationTestDb"));
            });

            builder.UseEnvironment("Testing");
        }
    }

    public class UserValidationIntegrationTests : IClassFixture<UserValidationWebAppFactory>
    {
        private readonly HttpClient _client;

        public UserValidationIntegrationTests(UserValidationWebAppFactory factory)
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

        private static UserCreationDTO ValidUser() => new()
        {
            Name           = "Marko",
            Surname        = "Markovic",
            Email          = "marko@example.com",
            Password       = "Zx9$mQ2!vK7w",
            Username       = "markom_int",
            RoleId         = Guid.NewGuid(),
            OrganizationId = null
        };

        // Parse the body and assert it is exactly { "error": "<expected>" }:
        // the "error" property exists with the expected value and "message" does NOT.
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
        public async Task CreateUser_MalformedEmail_Returns400_WithEmailMessage()
        {
            var dto = ValidUser();
            // Passes the DTO's [EmailAddress] annotation but fails the repository's
            // stricter regex (no dot in the domain) -> the BACKEND repository is the gate.
            dto.Email = "user@localhost";

            var response = await _client.PostAsJsonAsync("/api/User", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await AssertErrorBody(response, "Please enter a valid email address.");
        }

        [Fact]
        public async Task CreateUser_EmptyEmail_Returns400_WithEmailMessage_AndErrorShape()
        {
            var dto = ValidUser();
            dto.Email = "";   // empty -> repo email guard fires with canonical message

            var response = await _client.PostAsJsonAsync("/api/User", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await AssertErrorBody(response, "Please enter a valid email address.");
        }

        [Fact]
        public async Task CreateUser_EmptyPassword_Returns400_WithTooShortMessage_AndErrorShape()
        {
            var dto = ValidUser();
            dto.Email    = $"emptypw_{Guid.NewGuid():N}@example.com";
            dto.Username = $"emptypw_{Guid.NewGuid():N}".Substring(0, 20);
            dto.Password = "";   // empty -> password too-short guard fires

            var response = await _client.PostAsJsonAsync("/api/User", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await AssertErrorBody(response, "Password must be at least 8 characters.");
        }

        [Fact]
        public async Task CreateUser_WeakPassword_MissingLowercase_Returns400_WithComplexityMessage()
        {
            var dto = ValidUser();
            // Has uppercase, digit, special (so it satisfies the DTO password annotation)
            // but is missing lowercase -> fails the repository's full complexity policy.
            dto.Password = "ABCDEF1!";

            var response = await _client.PostAsJsonAsync("/api/User", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await AssertErrorBody(response,
                "Password must include uppercase, lowercase, a number, and a special character.");
        }

        [Fact]
        public async Task CreateUser_BreachedPassword_Returns400_WithBreachMessage()
        {
            var dto = ValidUser();
            dto.Email    = "breached@example.com";
            dto.Username = "breached_int";
            dto.Password = FakePwnedPasswordsClient.BreachedPassword;

            var response = await _client.PostAsJsonAsync("/api/User", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await AssertErrorBody(response,
                "This password has appeared in a data breach. Please choose another.");
        }

        [Fact]
        public async Task CreateUser_StrongUniqueNonBreached_Returns201()
        {
            var dto = ValidUser();
            dto.Email    = $"unique_{Guid.NewGuid():N}@example.com";
            dto.Username = $"unique_{Guid.NewGuid():N}".Substring(0, 20);
            dto.Password = "Zx9$mQ2!vK7w"; // strong, not the designated breached password

            var response = await _client.PostAsJsonAsync("/api/User", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<UserCreatedDTO>();
            Assert.NotNull(created);
            Assert.Equal(dto.Email.ToLower(), created!.Email);
        }
    }
}
