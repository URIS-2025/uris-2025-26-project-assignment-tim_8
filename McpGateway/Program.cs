using System.Text;
using McpGateway.Audit;
using McpGateway.Authorization;
using McpGateway.Configuration;
using McpGateway.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);

// ── Load + validate policy config (fail-fast at startup on bad config) ───────────
var gatewayConfig = builder.Configuration.GetSection("Gateway").Get<GatewayConfig>() ?? new GatewayConfig();
var policyStore = new PolicyStore(
    gatewayConfig.Tools.Select(t => t.ToDomain()),
    gatewayConfig.Agents.Select(a => a.ToDomain()));

// ── Security services (the gateway "brain") ──────────────────────────────────────
builder.Services.AddSingleton(policyStore);
builder.Services.AddSingleton(new AgentTokenValidator(policyStore.Agents));
builder.Services.AddSingleton(new ToolAuthorizer(policyStore.ToolPolicies));

// ── Durable audit store (Faza C): EF-backed sink + queryable context ─────────────
// A context FACTORY (not a scoped DbContext) so the SINGLETON sink/gatekeeper never hold one captive.
// SqlServer in production; InMemory under Testing so WebApplicationFactory boots without a real DB.
var isTestingEnv = builder.Environment.IsEnvironment("Testing");
builder.Services.AddDbContextFactory<AuditDbContext>(options =>
{
    if (isTestingEnv)
        options.UseInMemoryDatabase("McpAuditDB-Testing");
    else
        options.UseSqlServer(builder.Configuration.GetConnectionString("McpAuditDB")
            ?? throw new InvalidOperationException("Nedostaje ConnectionStrings:McpAuditDB u konfiguraciji."));
});
builder.Services.AddSingleton<IAuditSink, SqlAuditSink>();

builder.Services.AddSingleton<RequestGatekeeper>();
builder.Services.AddSingleton<ToolAuthorizationFilter>();
builder.Services.AddHttpContextAccessor();
// Per-request holder that carries the authorized execution context (org-scope + OBO bearer) from
// the authorization filter to the tool body. Scoped: filter and tool share the same request scope.
builder.Services.AddScoped<IInvocationContextAccessor, InvocationContextAccessor>();

// ── Read data layer: SELECT-only login over scrubbed, org-scoped views (Dapper) ──────────────────
var readDbOptions = new McpGateway.Data.ReadDbOptions
{
    ProblemDb       = builder.Configuration["ReadDb:ProblemDB"] ?? "",
    SuggestionDb    = builder.Configuration["ReadDb:SuggestionDB"] ?? "",
    ProblemBoxDb    = builder.Configuration["ReadDb:ProblemBoxDB"] ?? "",
    SuggestionBoxDb = builder.Configuration["ReadDb:SuggestionBoxDB"] ?? "",
};
// Fail fast (like the Gateway policy config): a missing ReadDb connection string would otherwise
// surface much later as an opaque SqlConnection(null) on the first tool call.
if (string.IsNullOrWhiteSpace(readDbOptions.ProblemDb) || string.IsNullOrWhiteSpace(readDbOptions.SuggestionDb)
    || string.IsNullOrWhiteSpace(readDbOptions.ProblemBoxDb) || string.IsNullOrWhiteSpace(readDbOptions.SuggestionBoxDb))
{
    throw new InvalidOperationException(
        "Nedostaje ReadDb konfiguracija — postavi ReadDb:{ProblemDB,SuggestionDB,ProblemBoxDB,SuggestionBoxDB}.");
}
// Fail fast on an EMPTY read-login password too. An unset ${MCP_READ_PASSWORD} in Docker leaves a
// syntactically valid "…User Id=mcp_read;Password=;…" that would otherwise boot "healthy" and only
// fail on the FIRST read tool call with an opaque "Login failed for user 'mcp_read'". Surfacing it
// at boot aligns MCP_READ_PASSWORD with the other injected secrets (which already crash-loop when unset).
foreach (var (name, cs) in new[]
{
    ("ReadDb:ProblemDB", readDbOptions.ProblemDb),
    ("ReadDb:SuggestionDB", readDbOptions.SuggestionDb),
    ("ReadDb:ProblemBoxDB", readDbOptions.ProblemBoxDb),
    ("ReadDb:SuggestionBoxDB", readDbOptions.SuggestionBoxDb),
})
{
    if (string.IsNullOrEmpty(new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(cs).Password))
        throw new InvalidOperationException(
            $"{name}: prazna lozinka za SELECT-only nalog — postavi MCP_READ_PASSWORD (env/.env) pre pokretanja.");
}
builder.Services.AddSingleton(readDbOptions);
builder.Services.AddScoped<McpGateway.Data.IReadRepository, McpGateway.Data.SqlReadRepository>();

// ── Write data layer: named HttpClients + typed clients that call the existing REST endpoints ────
// The write tools forward the caller's OBO bearer to these. Base URLs (Docker container names on
// :8080) come from the "Services" config section; a missing one is a fail-fast at startup.
string ServiceBaseUrl(string key) =>
    builder.Configuration[$"Services:{key}"]
    ?? throw new InvalidOperationException($"Nedostaje Services:{key} bazni URL u konfiguraciji.");

builder.Services.AddHttpClient("ProblemBoxService",    c => c.BaseAddress = new Uri(ServiceBaseUrl("ProblemBoxService")));
builder.Services.AddHttpClient("SuggestionBoxService", c => c.BaseAddress = new Uri(ServiceBaseUrl("SuggestionBoxService")));
builder.Services.AddHttpClient("ProblemService",       c => c.BaseAddress = new Uri(ServiceBaseUrl("ProblemService")));
builder.Services.AddHttpClient("SuggestionService",    c => c.BaseAddress = new Uri(ServiceBaseUrl("SuggestionService")));

builder.Services.AddScoped<McpGateway.Clients.ProblemBoxServiceClient>();
builder.Services.AddScoped<McpGateway.Clients.SuggestionBoxServiceClient>();
builder.Services.AddScoped<McpGateway.Clients.ProblemServiceClient>();
builder.Services.AddScoped<McpGateway.Clients.SuggestionServiceClient>();

// ── User (on-behalf-of) JWT: validate OrganizationService-issued tokens ──────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            // Pin the authz-critical claim types (OrganizationService emits ClaimTypes.Role /
            // NameIdentifier) so role/user reads don't silently depend on framework defaults.
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
        };
    });
builder.Services.AddAuthorization();

// ── REST controllers (audit review endpoint: GET /api/Audit) ─────────────────────
builder.Services.AddControllers();

// ── MCP server + the per-tool authorization filter (runs on EVERY tool call) ─────
builder.Services.AddMcpServer()
    .WithHttpTransport(o => o.Stateless = true)
    .WithToolsFromAssembly()
    .WithRequestFilters(filters =>
    {
        filters.AddCallToolFilter(next => async (context, cancellationToken) =>
        {
            var services = context.Services;
            if (services is null)
            {
                // No DI scope on the request → we cannot authorize or audit → fail closed.
                return new CallToolResult
                {
                    IsError = true,
                    Content = [new TextContentBlock { Text = "Zabranjeno: nedostaje kontekst servisa" }],
                };
            }

            var filter = services.GetRequiredService<ToolAuthorizationFilter>();
            var invocationContext = services.GetRequiredService<IInvocationContextAccessor>();
            var http = services.GetService<IHttpContextAccessor>()?.HttpContext;
            var agentToken = http?.Request.Headers["X-Agent-Token"].ToString();
            var bearer = http?.Request.Headers["Authorization"].ToString();
            var toolName = context.Params?.Name ?? string.Empty;

            // One logical AiChat interaction shares a correlation id across its whole chain of tool
            // calls; the demo agent forwards it here. Unparseable/absent → null (fail-safe).
            var correlationId = Guid.TryParse(http?.Request.Headers["X-Correlation-Id"].ToString(), out var cid)
                ? cid
                : (Guid?)null;

            return await filter.EvaluateAsync(
                agentToken, context.User, toolName,
                argsSummary: ArgsSummary.Build(context.Params?.Arguments),
                next: ct => next(context, ct), cancellationToken,
                invocationContext: invocationContext, bearerToken: bearer,
                correlationId: correlationId);
        });
    });

builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(8080));

var app = builder.Build();

// ── Apply AuditDB migrations on startup (skipped under Testing / InMemory) ────────
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    using var db = scope.ServiceProvider
        .GetRequiredService<IDbContextFactory<AuditDbContext>>().CreateDbContext();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

// The MCP endpoint requires an authenticated (OBO) user — unauthenticated → 401.
app.MapMcp("/mcp").RequireAuthorization();

// REST controllers (AuditController carries its own [Authorize(Roles=...)]).
app.MapControllers();

app.Run();

// Exposes the top-level Program type to WebApplicationFactory-based integration tests.
public partial class Program { }
