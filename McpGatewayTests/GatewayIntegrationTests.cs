using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using McpGateway.Audit;
using McpGateway.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace McpGatewayTests;

/// <summary>
/// Boots the whole McpGateway with WebApplicationFactory (which also proves it starts cleanly:
/// config binding + PolicyStore validation + agent public-key parsing), and verifies the MCP
/// endpoint is closed to unauthenticated / invalid callers.
/// </summary>
public class GatewayIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GatewayIntegrationTests(WebApplicationFactory<Program> factory) => _factory = factory;

    // A minimal MCP "tools/call" JSON-RPC body.
    private static StringContent McpCall() => new(
        """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"get_org_overview"}}""",
        Encoding.UTF8, "application/json");

    [Fact] // T8 — no OBO user token → the MCP endpoint rejects the call
    public async Task Mcp_endpoint_returns_401_without_a_user_token()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/mcp", McpCall());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact] // T9 — a tampered / invalid user token is rejected
    public async Task Mcp_endpoint_returns_401_with_an_invalid_token()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.valid.jwt");

        var response = await client.PostAsync("/mcp", McpCall());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Mint an OrganizationService-shaped OBO user token signed with the gateway's configured Jwt:Key.
    private static string MintUserToken(string role, Guid orgId)
    {
        const string key = "OrganizationService-Docker-Secret-Key-tim8-uris-2026-secure!!"; // matches appsettings.json
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(ClaimTypes.Role, role),
            new Claim("OrganizationId", orgId.ToString()),
        };
        var token = new JwtSecurityToken("OrganizationService", "OrganizationService", claims,
            expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact] // End-to-end: authenticated user but NO agent token → the per-tool filter denies + audits
    public async Task Authenticated_call_without_agent_token_is_denied_and_audited()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", MintUserToken("Manager", Guid.NewGuid()));
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/event-stream");
        // deliberately NO X-Agent-Token → the gateway must deny inside the call-tool filter.

        await client.PostAsync("/mcp", McpCall());

        // The decision is recorded by the singleton audit sink the running app uses.
        var sink = (InMemoryAuditSink)_factory.Services.GetRequiredService<IAuditSink>();
        Assert.Contains(sink.Entries, e => e.Decision == AuthDecision.Deny);
    }
}
