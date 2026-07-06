using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Anthropic;
using AiAssistantService.Agent;
using AiAssistantService.Auth;
using AiAssistantService.Clients;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Agent config (loop knobs, write allow-list, agent-credential) ─────────────────
builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection(AgentOptions.SectionName));

// Fail-fast on missing secrets (except under Testing, where the test host supplies its own).
var isTestingEnv = builder.Environment.IsEnvironment("Testing");
if (!isTestingEnv)
{
    if (string.IsNullOrWhiteSpace(builder.Configuration[$"{AgentOptions.SectionName}:PrivateKeyPem"]))
        throw new InvalidOperationException("Nedostaje Agent:PrivateKeyPem (agent privatni kljuc) — postavi preko env/user-secrets.");
    if (string.IsNullOrWhiteSpace(builder.Configuration["Anthropic:ApiKey"]))
        throw new InvalidOperationException("Nedostaje Anthropic:ApiKey — postavi preko env/user-secrets.");
    // Fail LOUD on a missing OBO signing key — otherwise a bogus fallback key would silently reject
    // every real OrganizationService token (all requests 401) with a clean boot log.
    if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]))
        throw new InvalidOperationException("Nedostaje Jwt:Key (OBO potpisni kljuc) — postavi preko env/user-secrets.");
}

// ── Anthropic SDK client (secret key from env / user-secrets) ─────────────────────
// Factory registration (lazy): the client is constructed only the first time it is actually
// resolved — i.e. on the first real AiChat request that reaches ClaudeClient — never eagerly at
// startup. Under Testing the agent is mocked and this factory is never invoked, so an empty
// ApiKey can never reach a live Anthropic client.
builder.Services.AddSingleton(_ => new AnthropicClient
{
    ApiKey = builder.Configuration["Anthropic:ApiKey"] ?? string.Empty,
});

// ── Agent services ────────────────────────────────────────────────────────────────
var mcpUrl = builder.Configuration["Services:McpGatewayUrl"]
    ?? throw new InvalidOperationException("Nedostaje Services:McpGatewayUrl bazni URL u konfiguraciji.");

builder.Services.AddSingleton<IAgentTokenService, AgentTokenService>();
builder.Services.AddSingleton<IClaudeAgentClient, ClaudeClient>();
builder.Services.AddSingleton<IMcpGatewayClient>(_ => new McpGatewayClient(mcpUrl));
builder.Services.AddScoped<IAiChatAgent, AiChatAgent>();

// ── User (on-behalf-of) JWT: validate OrganizationService-issued tokens ───────────
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("Nedostaje Jwt:Key."))),
            // Pin the authz-critical claim types (OrganizationService emits ClaimTypes.Role /
            // NameIdentifier) so role/user reads don't silently depend on framework defaults.
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier,
        };
    });
builder.Services.AddAuthorization();

// ── Rate limiting: 10 requests / minute, partitioned per authenticated user ───────
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("aichat", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Previse zahteva. Pokusajte ponovo za koji trenutak." }, ct);
    };
});

builder.Services.AddControllers();

builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(8080));

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (error != null)
            await context.Response.WriteAsJsonAsync(new { message = error.Error.Message });
    });
});

// X-Forwarded-For populated before anything reads it (nginx forwards it in Faza E).
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});

// Authentication BEFORE the rate limiter — the "aichat" policy partitions by the authenticated
// user's NameIdentifier, which is only populated once authentication has run.
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();

// Exposes the top-level Program type to WebApplicationFactory-based integration tests.
public partial class Program { }
