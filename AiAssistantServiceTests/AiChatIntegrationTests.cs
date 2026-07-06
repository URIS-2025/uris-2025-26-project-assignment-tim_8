using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using AiAssistantService.Agent;
using AiAssistantService.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AiAssistantServiceTests;

/// <summary>
/// Boots the whole AiAssistantService with WebApplicationFactory (proving it starts cleanly under
/// Testing) and verifies the endpoint's auth gate and per-user rate limit.
/// </summary>
public class AiChatIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AiChatIntegrationTests(WebApplicationFactory<Program> factory) =>
        _factory = factory.WithWebHostBuilder(b => b.UseEnvironment("Testing"));

    // OrganizationService-shaped OBO token signed with the gateway/AiAssistantService Jwt:Key.
    private static string MintUserToken()
    {
        const string key = "OrganizationService-Docker-Secret-Key-tim8-uris-2026-secure!!"; // matches appsettings.json
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(ClaimTypes.Role, "Manager"),
            new Claim("OrganizationId", Guid.NewGuid().ToString()),
        };
        var token = new JwtSecurityToken("OrganizationService", "OrganizationService", claims,
            expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact] // T9 — no OBO user token → the endpoint rejects the call
    public async Task AiChat_returns_401_without_a_user_token()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/AiChat", new AiChatRequestDTO { Message = "zdravo" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact] // T8 — the 11th request in a minute (per user) is rate-limited
    public async Task AiChat_rate_limits_after_ten_requests_per_user()
    {
        // Replace the agent with a fast stub so the rate limiter — not a real gateway call — is what
        // we exercise. The limiter runs before the endpoint, so it counts every admitted request.
        var factory = _factory.WithWebHostBuilder(b =>
            b.ConfigureServices(services =>
            {
                services.RemoveAll<IAiChatAgent>();
                services.AddScoped<IAiChatAgent, StubAgent>();
            }));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MintUserToken());

        // First 10 are admitted (200), the 11th is rejected (429).
        for (var i = 0; i < 10; i++)
        {
            var ok = await client.PostAsJsonAsync("/api/AiChat", new AiChatRequestDTO { Message = "x" });
            Assert.NotEqual(HttpStatusCode.TooManyRequests, ok.StatusCode);
        }

        var rejected = await client.PostAsJsonAsync("/api/AiChat", new AiChatRequestDTO { Message = "x" });
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    private sealed class StubAgent : IAiChatAgent
    {
        public Task<AiChatResponseDTO> RunAsync(AiChatRequestDTO request, string oboBearer, CancellationToken cancellationToken) =>
            Task.FromResult(new AiChatResponseDTO { Status = AiChatStatus.Completed, AssistantText = "ok", CorrelationId = "c" });
    }
}
