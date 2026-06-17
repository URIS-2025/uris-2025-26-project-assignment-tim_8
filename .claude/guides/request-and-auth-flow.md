# Request and Auth Flow

End-to-end trace of how a request travels through the Anonymous Reporting System, how
tokens are issued and validated, and where rate limiting is enforced.

Cross-references:
- Rule: `.claude/rules/controllers-and-errors.md` — controller pattern, error shapes
- Rule: `.claude/rules/auth-jwt.md` — JWT configuration, `[Authorize]` scope
- Guide: `.claude/guides/cross-service-contracts.md` — `LogCreationDTO`, HttpClient styles

---

## 1. Write-Request Lifecycle: `POST /api/Problem`

The numbered steps below trace a real request using `ProblemController.CreateProblem`,
`ProblemRepository.CreateProblem`, and `LoggerServiceClient.TryLogAsync`.

```
Browser / Frontend
    │
    │  POST /api/Problem
    │  Authorization: Bearer <jwt>
    ▼
┌─────────────────────────────────────────────────────┐
│  Nginx API Gateway  (gateway/nginx.conf)            │
│                                                     │
│  server block (listen 80):                         │
│    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for
│    proxy_set_header X-Forwarded-Proto $scheme       │
│    proxy_set_header Host $host                      │
│    proxy_set_header X-Real-IP $remote_addr          │
│                                                     │
│  location /api/Problem/ {                           │
│    add_header Access-Control-Allow-Origin "*"       │
│    proxy_pass http://problem_service/api/Problem/   │
│  }                                                  │
│                                                     │
│  upstream problem_service → problem-service:8080    │
└──────────────────────────┬──────────────────────────┘
                           │  (all original headers forwarded,
                           │   CORS added, TCP to container)
                           ▼
┌─────────────────────────────────────────────────────┐
│  ProblemService  (Kestrel :8080)                    │
│                                                     │
│  Middleware pipeline (ProblemService/Program.cs):   │
│    UseExceptionHandler → global 500 {message}       │
│    UseAuthorization   (no JWT handler registered!)  │
│    MapControllers                                   │
└──────────────────────────┬──────────────────────────┘
                           │
                           ▼
Step 1  ProblemController.CreateProblem([FromBody] ProblemCreationDTO)
        ── [HttpPost], [ApiController], [Route("api/[controller]")]
        ── No [Authorize] on this endpoint (ProblemService has no AddJwtBearer)

Step 2  _problemRepository.CreateProblem(problem)
        ── ArgumentException guards:
             if ProblemBoxId == Guid.Empty  → throw
             if Title is null/empty         → throw
             if Description is null/empty   → throw
        ── _mapper.Map<Problem>(problem)    creates entity
        ── entity.Id        = Guid.NewGuid()
        ── entity.CreatedAt = DateTime.UtcNow
        ── _context.Problems.Add(entity)
        ── SaveChanges() → _context.SaveChanges() > 0

Step 3  _mapper.Map<ProblemCreatedDTO>(entity)
        ── AutoMapper maps entity → DTO (registered via AddMaps(typeof(Program).Assembly))
        ── Entity is NEVER returned directly; only the DTO escapes the repo

Step 4  Audit log — fire-and-forget side channel
        ── await _loggerClient.TryLogAsync(
               new LogCreationDTO {
                   UserId      = User.Identity?.Name,
                   Action      = "CREATE_PROBLEM",
                   EntityName  = "Problem",
                   NewValues   = JsonSerializer.Serialize(result),   // the DTO
                   IsSuccess   = true,
                   ServiceName = "ProblemService",
                   HttpMethod  = "POST"
               },
               Request.Headers["Authorization"],   // raw "Bearer <jwt>" string
               HttpContext.RequestAborted)
        ── Inside TryLogAsync (LoggerServiceClient):
               factory.CreateClient("LoggerService")   → named HttpClient
               BaseAddress from appsettings:"Services:LoggerServiceBaseUrl"
               req.Headers.Authorization = AuthenticationHeaderValue.Parse(bearerHeader)
               CancellationTokenSource(800 ms) linked with request CT
               await _http.SendAsync(req, linked.Token)
               catch { }   ← swallows ALL errors; logging failure never surfaces to caller

Step 5  return Created("", result)
        ── HTTP 201 with ProblemCreatedDTO body

On exception in Step 2 or 3:
        ── await _loggerClient.TryLogAsync(... IsSuccess=false ...)
        ── return BadRequest(new { error = ex.Message })   ← key is "error"

Uncaught exception (no catch block reached):
        ── UseExceptionHandler → HTTP 500 { message = ex.Message }  ← key is "message"
```

> Note: the `TryLogAsync` call is `await`-ed but the method swallows every exception
> internally (`catch { }`), so it behaves effectively as fire-and-forget from the
> caller's perspective — logging failure cannot cause the endpoint to fail.

---

## 2. Auth Flow: Token Issuance, Storage, and Validation

### 2.1 Who issues JWTs

Two services issue tokens; all others are consumers only.

| Service | Issuer | Repository method |
|---|---|---|
| AnonymousUserService | `AnonymousUserController` → `AnonymousUserRepository.Login` | `GenerateTokenPair` |
| OrganizationService | `UserController` → `UserRepository.Login` | (identical pattern) |

### 2.2 What login returns

Both services return the same shape from `GenerateTokenPair`:

```json
{
  "accessToken":  "<JWT, 8-hour expiry>",
  "refreshToken": "<opaque 64-byte random Base64, 7-day expiry>"
}
```

- `accessToken` — a signed `JwtSecurityToken` (HMAC-SHA256) containing claims
  `ClaimTypes.NameIdentifier` (user GUID) and `ClaimTypes.Name` (username).
- `refreshToken` — `RandomNumberGenerator.GetBytes(64)` encoded as Base64. Opaque; not a JWT.

### 2.3 Refresh token storage

Both services store refresh tokens in their own SQL Server database. The models are
structurally identical (copy-pasted, different namespace):

| Model | Namespace | Service |
|---|---|---|
| `AnonymousRefreshToken` | `AnonymousDomain.Models.AnonymousUser` | AnonymousUserService |
| `RefreshToken` | `AnonymousDomain.Models.Organization` | OrganizationService |

Fields in both:

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK to the user row |
| `Token` | `string` | The opaque Base64 value |
| `CreatedAt` | `DateTime` | Issuance timestamp |
| `ExpiresAt` | `DateTime` | `UtcNow + 7 days` |
| `IsRevoked` | `bool` | Set to `true` on use (rotation) |

On refresh (`AnonymousUserRepository.RefreshToken`):
1. Look up the stored token: `Token == refreshToken && !IsRevoked && ExpiresAt > UtcNow`.
2. If not found → `UnauthorizedAccessException`.
3. Mark `IsRevoked = true` and `SaveChanges()` (rotation; one-time use).
4. Call `GenerateTokenPair` again → new access + refresh token pair returned.

### 2.4 JWT validation — only 2 services validate

JWT bearer validation is configured **only** in:
- `AnonymousUserService/Program.cs:69-83` — `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`
- `OrganizationService/Program.cs:71-85` — identical configuration

Both validate: issuer, audience, lifetime, signing key (symmetric HMAC-SHA256). Key material
comes from `appsettings.json:"Jwt:Key"`, `"Jwt:Issuer"`, `"Jwt:Audience"`.

Middleware pipeline order in both auth services (matters):

```
UseForwardedHeaders   ← trust X-Forwarded-For / X-Forwarded-Proto from gateway
UseRateLimiter        ← checked before any auth
UseAuthentication     ← validates Bearer token, populates ClaimsPrincipal
UseAuthorization      ← enforces [Authorize] attributes
MapControllers
```

The other 9 services (ProblemService, SuggestionService, etc.) call only `UseAuthorization`
with no `AddAuthentication` / `AddJwtBearer` registration. `[Authorize]` on endpoints in
those services would have no effect.

> **Security note:** The 9 non-authenticating services trust that Nginx is the only
> ingress point. Any request that reaches a service container directly (bypassing the
> gateway) carries no enforced identity. Direct container-to-container calls also skip
> JWT validation. In production this is mitigated only by Docker network isolation
> (services are not exposed on the host). If a container is ever exposed — intentionally
> or by misconfiguration — it is fully open. See `.claude/rules/auth-jwt.md` for the
> full list of services where `[Authorize]` is active vs. inert.

---

## 3. Rate-Limit Path

Rate limiting is configured **identically** in both auth services:

```
AnonymousUserService/Program.cs:30-62
OrganizationService/Program.cs:32-64
```

### Policies

| Policy name | Limit | Window | Applied to |
|---|---|---|---|
| `"login"` | 5 requests | per 1 minute | `POST /api/AnonymousUser/login`, `POST /api/User/login` |
| `"register"` | 3 requests | per 1 hour | `POST /api/AnonymousUser/register`, `POST /api/User/register` |

### Partition key

```csharp
partitionKey: httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded)
    ? forwarded.ToString()
    : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
```

The key is the `X-Forwarded-For` header value when present, falling back to the TCP
remote IP. Because Nginx injects `X-Forwarded-For: $proxy_add_x_forwarded_for` (gateway
`nginx.conf:66`) and the services call `UseForwardedHeaders`, the partition key resolves
to the **original client IP address**, not the gateway's IP. This ensures rate limits are
per-client, not per-gateway.

Both services also call `app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto })` before `UseRateLimiter`, so the forwarded IP is available in `HttpContext.Connection.RemoteIpAddress` as well.

### On rejection

```json
HTTP 429
{ "error": "Too many requests. Please try again later." }
```

The `error` key matches the controller catch-block convention (not the global handler's
`message` key). See `.claude/rules/controllers-and-errors.md` for the full error-key
inconsistency.

---

## 4. Bearer Header Forwarding in Service-to-Service Calls

When a controller calls `LoggerServiceClient.TryLogAsync`, it passes
`Request.Headers["Authorization"]` — the raw `Bearer <jwt>` string from the original
inbound request. The client copies this verbatim onto the outgoing HTTP request:

```csharp
req.Headers.Authorization = AuthenticationHeaderValue.Parse(bearerHeader);
```

LoggerService accepts all POST requests with `[AllowAnonymous]` (it does not validate the
token), but the header is forwarded to maintain an audit trail of which identity triggered
the log entry. This is the `Clients/` Style A pattern described in
`.claude/guides/cross-service-contracts.md`.

---

## Sources

| File | Why read |
|---|---|
| `ProblemService/Controllers/ProblemController.cs` | CreateProblem endpoint implementation, TryLogAsync call sites |
| `ProblemService/Data/ProblemRepository.cs` | CreateProblem implementation, SaveChanges, mapper usage |
| `ProblemService/Clients/LoggerServiceClient.cs` | TryLogAsync implementation, bearer forwarding, timeout, error swallow |
| `ProblemService/Program.cs` | Middleware pipeline order, HttpClient registrations, global exception handler |
| `AnonymousUserService/Program.cs` | Rate limiter policies, JWT bearer configuration, ForwardedHeaders, middleware order |
| `AnonymousUserService/Data/AnonymousUserRepository.cs` | Login, GenerateTokenPair, GenerateAccessToken, IssueRefreshToken, RefreshToken |
| `AnonymousUserService/Models/AnonymousUser/AnonymousRefreshToken.cs` | Refresh token entity fields |
| `OrganizationService/Models/RefreshToken.cs` | Org-side refresh token entity (structurally identical) |
| `OrganizationService/Program.cs` | Confirmed identical rate limiter + JWT bearer configuration |
| `gateway/nginx.conf` | Upstream blocks, proxy_set_header X-Forwarded-For, CORS headers, location routing |
| `.coordination/SCAN_REPORT.md` | Authoritative inventory: auth service count, rate-limit correction, [Authorize] count |
