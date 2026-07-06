# Rule: Auth & JWT

Cross-link: `.claude/rules/program-bootstrap.md`

## Quick summary

| Concern | Services |
|---|---|
| JWT bearer validation (`AddAuthentication` + `AddJwtBearer`) | **2 only**: AnonymousUserService, OrganizationService |
| Rate limiting (`AddRateLimiter` with `login`/`register` policies) | **2 only**: AnonymousUserService, OrganizationService |
| `[Authorize]` active (action-level) | **13 occurrences across 4 controllers** (see table below) |
| `//[Authorize]` commented out | 2 files — **no effect** |
| Services with NO auth wiring (9) | ProblemService, ProblemBoxService, SuggestionService, SuggestionBoxService, SubscriptionService, BillingNotificationService, SystemNotificationService, AttachmentService, LoggerService |

---

## JWT registration block

Both services use an **identical** `AddAuthentication` + `AddJwtBearer` block placed AFTER
`AddControllers()` and BEFORE `builder.Build()`.

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer     = builder.Configuration["Jwt:Issuer"],
            ValidAudience   = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
```

Evidence:
- `AnonymousUserService/Program.cs:69-83`
- `OrganizationService/Program.cs:71-85`

### appsettings.json shape (both services)

```json
"Jwt": {
  "Key":      "<secret-32+-chars>",
  "Issuer":   "<ServiceName>",
  "Audience": "<ServiceName>"
}
```

Evidence: `AnonymousUserService/appsettings.json:15-19`

**Production note:** the dev key in `AnonymousUserService/appsettings.json` ("AnonymousUserService-Dev-Key-Replace-In-Production-32chars!") must be replaced via environment variable or secrets before deployment. The HMAC-SHA256 key must be ≥32 UTF-8 bytes.

---

## Middleware pipeline order

Both JWT services follow the same pipeline order in `app.*` calls:

```
UseForwardedHeaders(...)     // MUST be first — populates X-Forwarded-For before rate limiter reads it
UseRateLimiter()             // reads X-Forwarded-For for partition key
UseAuthentication()          // MUST come before UseAuthorization
UseAuthorization()
MapControllers()
```

Evidence: `AnonymousUserService/Program.cs:100-108`, `OrganizationService/Program.cs:102-110`

WRONG order:
```csharp
app.UseAuthorization();    // WRONG: before UseAuthentication — [Authorize] never sees a principal
app.UseAuthentication();
```

---

## Active [Authorize] controllers

Only 4 controllers carry active (non-commented) `[Authorize]` attributes — all at **action level**, not class level.

| Controller | File | Active [Authorize] lines | Actions protected |
|---|---|---|---|
| `AnonymousUserController` | `AnonymousUserService/Controllers/AnonymousUserController.cs` | :28, :36, :109 | GET all, GET by id, DELETE |
| `UserController` | `OrganizationService/Controllers/UserController.cs` | :26, :34, :83, :122 | GET all, GET by id, PUT, DELETE |
| `UserRoleController` | `OrganizationService/Controllers/UserRoleController.cs` | :39, :76, :115 | GET all, GET by id, DELETE |
| `OrganizationController` | `OrganizationService/Controllers/OrganizationController.cs` | :39, :76, :115 | GET all, GET by id, DELETE |

Total: **13 active occurrences across 4 controllers**.

### Commented-out [Authorize] — no effect

```
BillingNotificationService/Controllers/BillingNotificationController.cs:11   //[Authorize]
AnonymousUserService/Controllers/BoxAccessLinkController.cs:10               //[Authorize]
```

These are **dead code** — they do not enforce auth. `BillingNotificationService` also has NO
`AddAuthentication` registration, so enabling the attribute there would fail silently (no 401
— returns 200 with no identity) until JWT wiring is added to its `Program.cs`.

---

## Token issuance and password hashing — REPOS, not controllers

Token generation and credential verification live entirely inside repository classes, never in controllers.

### AnonymousUserService

File: `AnonymousUserService/Data/AnonymousUserRepository.cs`

| Method | Visibility | Purpose |
|---|---|---|
| `Login(AnonymousUserLoginDTO)` | public | Verifies BCrypt hash, calls `GenerateTokenPair` |
| `RefreshToken(string)` | public | Validates stored refresh token, rotates, calls `GenerateTokenPair` |
| `GenerateTokenPair(AnonymousUser)` | private | Calls `GenerateAccessToken` + `IssueRefreshToken`, returns `AnonymousLoginResponseDTO {AccessToken, RefreshToken}` |
| `GenerateAccessToken(AnonymousUser)` | private | Builds `JwtSecurityToken` (HMAC-SHA256, 8 h expiry) with `NameIdentifier`+`Name` claims |
| `IssueRefreshToken(Guid)` | private | `RandomNumberGenerator.GetBytes(64)` → Base64, stored in DB, 7 day expiry |
| `CreateUser(AnonymousUserCreationDTO)` | public | Hashes password with `BCrypt.Net.BCrypt.HashPassword(...)` |

Evidence: `AnonymousUserService/Data/AnonymousUserRepository.cs:62` (hash), `:97-101` (`GenerateTokenPair`), `:104-124` (`GenerateAccessToken`), `:126-142` (`IssueRefreshToken`), `:116` (`JwtSecurityToken`)

### OrganizationService

File: `OrganizationService/Data/UserRepository.cs`

| Method | Visibility | Purpose |
|---|---|---|
| `Login(UserLoginDTO)` | public | BCrypt verify, calls `GenerateTokenPair` |
| `RefreshToken(string)` | public | Validates stored token, rotates, calls `GenerateTokenPair` |
| `GenerateTokenPair(User)` | private | Returns `LoginResponseDTO {AccessToken, RefreshToken}` |
| `GenerateAccessToken(User)` | private | Builds `JwtSecurityToken` with `NameIdentifier`, `Name`, `Email`, `Role`, `RoleId`, `OrganizationId` claims (includes role claim — richer than AnonymousUser token) |
| `IssueRefreshToken(Guid)` | private | Same pattern as AnonymousUserService (`RandomNumberGenerator.GetBytes(64)`, 7 day expiry) |
| `CreateUser(UserCreationDTO)` | public | `BCrypt.Net.BCrypt.HashPassword(user.Password)` |

Evidence: `OrganizationService/Data/UserRepository.cs:45` (hash), `:86-93` (`Login`), `:114-118` (`GenerateTokenPair`), `:121-146` (`GenerateAccessToken`), `:139` (`JwtSecurityToken`)

### Token shape differences

| Field | AnonymousUser token | Organization User token |
|---|---|---|
| `NameIdentifier` | `user.Id` | `user.Id` |
| `Name` | `user.Username` | `user.Username` |
| `Email` | — | `user.Email` |
| `Role` | — | `role?.Title ?? "user"` |
| `RoleId` | — | `user.RoleId` |
| `OrganizationId` | — | `user.OrganizationId` |
| Expiry | 8 hours | 8 hours |
| Signing | HMAC-SHA256 | HMAC-SHA256 |

---

## Rate limiting

Both services define **identical** `AddRateLimiter` blocks with two named policies.

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded)
                ? forwarded.ToString()
                : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window      = TimeSpan.FromMinutes(1),
                QueueLimit  = 0
            }));

    options.AddPolicy("register", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: /* same X-Forwarded-For logic */,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window      = TimeSpan.FromHours(1),
                QueueLimit  = 0
            }));

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Too many requests. Please try again later." }, ct);
    };
});
```

Evidence: `AnonymousUserService/Program.cs:30-62`, `OrganizationService/Program.cs:32-64`

### Rate-limit policies summary

| Policy name | Algorithm | Limit | Window | Partition key |
|---|---|---|---|---|
| `login` | Fixed window | 5 requests | 1 minute | `X-Forwarded-For` header (fallback: `RemoteIpAddress`) |
| `register` | Fixed window | 3 requests | 1 hour | `X-Forwarded-For` header (fallback: `RemoteIpAddress`) |

### Rejection response
HTTP 429, body `{ "error": "Too many requests. Please try again later." }` — consistent with the `error` key used by controller catch blocks.

### UseForwardedHeaders requirement
`UseForwardedHeaders` MUST run before `UseRateLimiter` so that the Nginx-forwarded client IP is already populated in `Request.Headers["X-Forwarded-For"]` when the rate-limiter partition key is evaluated.

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseRateLimiter();
```

Evidence: `AnonymousUserService/Program.cs:100-105`, `OrganizationService/Program.cs:102-107`

### Applying policies to endpoints
Use `[EnableRateLimiting("login")]` / `[EnableRateLimiting("register")]` on the action method:

```csharp
[HttpPost("login")]
[EnableRateLimiting("login")]
public async Task<ActionResult<LoginResponseDTO>> Login(...)

[HttpPost]
[EnableRateLimiting("register")]
public async Task<ActionResult<UserCreatedDTO>> CreateUser(...)
```

Evidence: `OrganizationService/Controllers/UserController.cs:161`, `AnonymousUserService/Controllers/AnonymousUserController.cs:44-45`

### Per-USER partition → `UseRateLimiter` AFTER `UseAuthentication`

The two auth services partition by `X-Forwarded-For` (a pre-auth value), so they call
`UseRateLimiter` **before** `UseAuthentication`. A policy partitioned by the **authenticated user**
instead — e.g. `AiAssistantService`'s `"aichat"` policy keyed on
`httpContext.User.FindFirst(ClaimTypes.NameIdentifier)` — MUST place `UseRateLimiter` **after**
`UseAuthentication`/`UseAuthorization`. Otherwise `HttpContext.User` is empty when the partition key
is evaluated and every caller falls into the same `"anonymous"` bucket (one shared global limit, not
per-user).

```csharp
// CORRECT for a per-user partition (opposite order from the IP-partitioned auth services)
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();   // User is populated → NameIdentifier partition works
app.MapControllers();
```

Evidence: `AiAssistantService/Program.cs` (`"aichat"` policy + pipeline order).

---

## WRONG / CORRECT patterns

### WRONG: [Authorize] in a service with no AddAuthentication

```csharp
// BillingNotificationService/Controllers/BillingNotificationController.cs
[ApiController]
[Route("api/[controller]")]
//[Authorize]            // COMMENTED OUT — currently dead
public class BillingNotificationController : ControllerBase { ... }
```

If you uncomment `[Authorize]` here (or add it to ANY of the 9 services that don't register JWT),
the middleware pipeline has no authentication handler to run. The request is processed as
anonymous — the attribute is effectively a no-op (no 401 is returned). Depending on ASP.NET
Core version and configuration, it may also return 500 if the authorization middleware cannot
find a scheme.

CORRECT: Before adding `[Authorize]` to a controller in any service, verify that service's
`Program.cs` contains the full JWT registration block. If it does not, add it first:

```csharp
// 1. Add NuGet: Microsoft.AspNetCore.Authentication.JwtBearer
// 2. In Program.cs, add the registration block (see JWT registration section above)
// 3. Add UseAuthentication() + UseAuthorization() in the pipeline
// 4. Only then add [Authorize] to controllers/actions
```

Services that currently lack auth wiring and would need the full registration block added:
ProblemService, ProblemBoxService, SuggestionService, SuggestionBoxService,
SubscriptionService, BillingNotificationService, SystemNotificationService,
AttachmentService, LoggerService.

### WRONG: [Authorize] at class level (current pattern uses action level)

The existing `[Authorize]` attributes are all at **action level**. This is intentional: the same
controller often exposes public endpoints (POST /login, POST /register) alongside protected ones
(GET, PUT, DELETE). Putting `[Authorize]` on the class would lock out login/register.

```csharp
// WRONG — locks out POST /login
[ApiController]
[Authorize]
public class UserController : ControllerBase { ... }

// CORRECT — existing pattern
[ApiController]
public class UserController : ControllerBase
{
    [Authorize]
    [HttpGet]
    public ActionResult<IEnumerable<UserDTO>> GetAllUsers() { ... }

    [HttpPost("login")]          // no [Authorize] — must be public
    [EnableRateLimiting("login")]
    public ActionResult<LoginResponseDTO> Login(...) { ... }
}
```

### WRONG: Issuing tokens in controllers

```csharp
// WRONG — token logic in a controller
[HttpPost("login")]
public IActionResult Login([FromBody] LoginDTO dto)
{
    var token = new JwtSecurityToken(...);   // never do this in a controller
    return Ok(new { token });
}
```

CORRECT: token issuance lives in `AnonymousUserRepository.GenerateAccessToken` /
`UserRepository.GenerateAccessToken`. Controllers call repo methods and return their results.

---

## Adding a new JWT-protected service — checklist

1. Add NuGet `Microsoft.AspNetCore.Authentication.JwtBearer` to the service.
2. Copy the `AddAuthentication(...).AddJwtBearer(...)` block from `AnonymousUserService/Program.cs:69-83`.
3. Add `"Jwt": { "Key": "...", "Issuer": "...", "Audience": "..." }` to `appsettings.json`.
4. In the pipeline, add `UseAuthentication()` BEFORE `UseAuthorization()`.
5. If rate limiting is needed, copy `AddRateLimiter` block and add `UseForwardedHeaders()` + `UseRateLimiter()` before `UseAuthentication()`.
6. Add `[Authorize]` to individual actions (not the class) unless ALL endpoints need auth.
7. Keep password hashing and token generation in the repository layer.
