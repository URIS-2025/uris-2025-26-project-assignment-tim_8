using System.Text;
using McpGateway.Audit;
using McpGateway.Authorization;
using McpGateway.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
builder.Services.AddSingleton<IAuditSink, InMemoryAuditSink>();
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
builder.Services.AddSingleton(readDbOptions);
builder.Services.AddScoped<McpGateway.Data.IReadRepository, McpGateway.Data.SqlReadRepository>();

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
        };
    });
builder.Services.AddAuthorization();

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

            return await filter.EvaluateAsync(
                agentToken, context.User, toolName,
                argsSummary: ArgsSummary.Build(context.Params?.Arguments),
                next: ct => next(context, ct), cancellationToken,
                invocationContext: invocationContext, bearerToken: bearer);
        });
    });

builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(8080));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// The MCP endpoint requires an authenticated (OBO) user — unauthenticated → 401.
app.MapMcp("/mcp").RequireAuthorization();

app.Run();

// Exposes the top-level Program type to WebApplicationFactory-based integration tests.
public partial class Program { }
