using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using McpGateway.Audit;
using McpGateway.Authorization;
using McpGateway.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
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

    // Boot under the "Testing" environment so the AuditDB uses the InMemory provider and startup
    // migration is skipped (no real SQL Server needed).
    public GatewayIntegrationTests(WebApplicationFactory<Program> factory) =>
        _factory = factory.WithWebHostBuilder(b => b.UseEnvironment("Testing"));

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

    [Fact] // The audit review endpoint is closed to unauthenticated callers ([Authorize(Roles=...)]).
    public async Task Audit_endpoint_returns_401_without_a_token()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/Audit");

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

        // End-to-end: the deny is durably persisted through SqlAuditSink into the (Testing = InMemory)
        // AuditDB, and is readable via the same context factory the AuditController uses.
        using var db = _factory.Services
            .GetRequiredService<IDbContextFactory<AuditDbContext>>().CreateDbContext();
        Assert.Contains(db.AuditEntries, e => e.Decision == AuthDecision.Deny);
    }

    /// <summary>
    /// An <see cref="IDbContextFactory{TContext}"/> that always throws, standing in for a real
    /// AuditDB failure (unreachable server, SQL timeout, migration drift).
    /// </summary>
    private sealed class ThrowingAuditContextFactory : IDbContextFactory<AuditDbContext>
    {
        // Message deliberately contains the kind of topology a SqlException would carry, so the
        // assertions below prove it is NOT echoed to the caller.
        public AuditDbContext CreateDbContext() =>
            throw new InvalidOperationException(
                "A network-related error occurred connecting to SQL Server 'sql-server' database 'McpAuditDB'.");
    }

    // D7 — an unhandled fault on the PUBLICLY routed /api/Audit/ must return a FIXED message, never
    // internals. The gateway previously had NO global exception handler while running with
    // ASPNETCORE_ENVIRONMENT=Development in docker-compose, so the developer exception page was
    // active and any AuditDB hiccup returned an HTML stack trace plus a request-header dump — from
    // the one component whose premise is not leaking internal detail.
    //
    // Scope note: this boots under "Testing", so it verifies the handler's own behaviour and
    // response shape. In Development the same handler is registered upstream of the endpoint and is
    // therefore still the innermost handler to see an endpoint fault, so it wins there too — that
    // ordering is reasoned, not asserted here (a Development boot would need a real SQL Server).
    [Fact]
    public async Task Unhandled_fault_returns_a_fixed_message_and_never_leaks_internals()
    {
        using var factory = _factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextFactory<AuditDbContext>>();
                services.AddSingleton<IDbContextFactory<AuditDbContext>, ThrowingAuditContextFactory>();
            });
        });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", MintUserToken("Admin", Guid.NewGuid()));

        var response = await client.GetAsync("/api/Audit");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("Interna greška gateway-a.", body);

        // The no-leak assertions — these are the point of the test.
        Assert.DoesNotContain("sql-server", body);          // no server name
        Assert.DoesNotContain("McpAuditDB", body);          // no database name
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.DoesNotContain("McpGateway.", body);         // no namespace / stack frames
        Assert.DoesNotContain("StackTrace", body);
    }

    [Fact] // Faza D — a client-supplied X-Correlation-Id is threaded into the audit record
    public async Task Correlation_id_header_is_recorded_on_the_audit_entry()
    {
        var correlationId = Guid.NewGuid();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", MintUserToken("Manager", Guid.NewGuid()));
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/event-stream");
        client.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId.ToString());

        await client.PostAsync("/mcp", McpCall());

        using var db = _factory.Services
            .GetRequiredService<IDbContextFactory<AuditDbContext>>().CreateDbContext();
        Assert.Contains(db.AuditEntries, e => e.CorrelationId == correlationId);
    }
}
