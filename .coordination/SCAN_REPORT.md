# SCAN_REPORT.md — Backend Inventory (Anonymous Reporting System)

Single source of truth for tasks 002–011. Every claim is backed by `file:line` grep/Read
evidence gathered against the `dev` snapshot on the `coordinate/bootstrap-knowledge-base`
branch. **Scope = backend** (11 ASP.NET Core 8 services + Nginx gateway). Frontend is out of
scope except the gateway contract.

> **Counting convention:** `.cs` counts EXCLUDE `obj/`, `bin/`, `*.Designer.cs`,
> `*ModelSnapshot.cs` (per the hard constraints). The task file's pre-seeded counts counted
> those, so several numbers below are **corrected** — see the "Corrected from task file"
> callouts.

---

## 1. Service inventory

| Service | `.cs` files | Purpose |
|---|---|---|
| AnonymousUserService | 26 | Anonymous auth, JWT + refresh-token issuance, box-access links. **Issues JWTs.** |
| OrganizationService | 36 | Organizations, users, roles. **Issues JWTs.** |
| ProblemService | 40 | Problem submissions, comments, categories |
| ProblemBoxService | 19 | Problem-box configuration |
| SuggestionService | 44 | Suggestions, votes, comments, categories |
| SuggestionBoxService | 15 | Suggestion-box configuration |
| SubscriptionService | 36 | Subscription plans, payments |
| BillingNotificationService | 17 | Billing notifications |
| SystemNotificationService | 15 | System notifications |
| AttachmentService | 15 | File uploads / attachments |
| LoggerService | 10 | Centralized audit log sink (all services POST here) |

> **Corrected from task file.** Task pre-seeded: AnonymousUser 35, Organization 44,
> Problem 46, ProblemBox 25, Suggestion 51, SuggestionBox 21, Subscription 42,
> BillingNotification 23, SystemNotification 21, Attachment 22, Logger 16. Those numbers
> include `obj/bin/Designer/Snapshot` (verified: a no-exclusion `find` reproduces them
> exactly). The table above is the clean count. All 11 services still have ≥10 source
> files — none should be folded.

Service count: **11** (verified — matches `gateway/nginx.conf` upstreams and
`docker-compose.yml`).

---

## 2. Shared code — there is NO shared project (gotcha)

There is no shared csproj / class library. Two forms of **copy-paste duplication** exist:

| Duplicated artifact | Copies | Evidence |
|---|---|---|
| `namespace AnonymousDomain.Models.*` model classes | **20 files** across 8 services | `grep -rl "namespace AnonymousDomain"` → 20 files incl. `OrganizationService/Models/User.cs:`, `SuggestionService/Models/Suggestion.cs:`, `AttachmentService/Models/Attachment.cs:`, `AnonymousUserService/Models/AnonymousUser/AnonymousUser.cs:` |
| `LogCreationDTO.cs` | **11 copies** (10 in `*/Clients/` + canonical `LoggerService/Models/DTOs/LogCreationDTO.cs`) | `**/Clients/LogCreationDTO.cs` glob → 10 hits |
| `LoggerServiceClient.cs` | **10 copies** (one per non-logger service that logs) | `**/Clients/LoggerServiceClient.cs` glob → 10 hits |

**Gotcha for rule/guide authors:** the `AnonymousDomain` namespace looks like a shared
library but is NOT — each service has its own physical copy. Editing one does not propagate.
Treat `AnonymousDomain.*` as "convention, not contract."

---

## 3. Inter-service communication — TWO divergent REST styles

Both styles coexist. Named HttpClients are registered per-`Program.cs` via `AddHttpClient(...)`
(**16 registrations across 10 `Program.cs` files** — verified). Target URLs live in each
service's `appsettings.json` under `"Services"`.

### Style A — `Clients/` (good): `IHttpClientFactory`, async, forwards bearer, fail-soft
Reference: `ProblemService/Clients/LoggerServiceClient.cs`.

| Trait | Detail | Line |
|---|---|---|
| Client source | `factory.CreateClient("LoggerService")` | `:14` |
| Async | `await _http.SendAsync(...)` | `:32` |
| Forwards bearer | `req.Headers.Authorization = AuthenticationHeaderValue.Parse(bearerHeader)` | `:23-24` |
| Timeout | `CancellationTokenSource(TimeSpan.FromMilliseconds(800))` | `:29` |
| Serializer | `System.Text.Json` (`JsonContent.Create`, Web defaults) | `:26-27` |
| Error handling | swallows everything: `catch { }` | `:34` |

### Style B — `ServiceCalls/` (anti-pattern): `new HttpClient()`, sync-over-async, no bearer
Reference: `ProblemService/ServiceCalls/AttachmentService.cs`.

| Trait | Detail | Line |
|---|---|---|
| Client source | `new HttpClient()` (NOT the factory) | `:17` |
| Sync-over-async | `client.GetAsync(url).Result` / `.ReadAsStringAsync().Result` | `:20`, `:25` |
| Bearer | none forwarded | — |
| URL | built from `_configuration["Services:AttachmentService"]` | `:19` |
| Serializer | `Newtonsoft.Json` (`JsonConvert.DeserializeObject`) | `:1,:26` |
| Error handling | returns `null` on non-2xx | `:21-23` |

`ServiceCalls/` exists in 5 services (11 files): `ProblemService`, `ProblemBoxService`,
`SuggestionService`, `SuggestionBoxService`, `SubscriptionService`
(e.g. `SubscriptionService/ServiceCalls/OrganizationServiceCall.cs`,
`SuggestionService/ServiceCalls/UserServiceCall.cs`).

**Divergence to encode as a rule:** `Clients/` = `System.Text.Json` + factory + async;
`ServiceCalls/` = `Newtonsoft.Json` + `new HttpClient()` + `.Result`. New code should follow
Style A.

---

## 4. Auth / security

| Concern | Where | Evidence |
|---|---|---|
| JWT bearer validation configured | **ONLY 2 services** | `AnonymousUserService/Program.cs:69-83`, `OrganizationService/Program.cs:71+` (`AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer`) |
| `[Authorize]` (active, uncommented) | **13 occurrences / 4 controllers** | `AnonymousUserService/Controllers/AnonymousUserController.cs` (3), `OrganizationService/Controllers/{UserController.cs(4),UserRoleController.cs(3),OrganizationController.cs(3)}` |
| `//[Authorize]` (commented out) | 2 more files | `BillingNotificationService/Controllers/BillingNotificationController.cs:11`, `AnonymousUserService/Controllers/BoxAccessLinkController.cs:10` |
| Rate limiting (`AddRateLimiter`, `login`+`register` policies) | **2 services** | `AnonymousUserService/Program.cs:30-62`, `OrganizationService/Program.cs:32-64` (identical: login 5/min, register 3/hr, OnRejected 429) |
| Password hashing + JWT issuance | 2 production files | `AnonymousUserService/Data/AnonymousUserRepository.cs` (`System.Security.Cryptography:8`, `JwtSecurityToken:116`, `GenerateTokenPair:97`), `OrganizationService/Data/UserRepository.cs` |

> **Corrected from task file (TWO corrections):**
> 1. Task says `[Authorize]` = "15 occurrences / 6 controllers / 6 files." **Active** count is
>    **13 / 4 files**. The 15/6 figure includes the 2 `//[Authorize]` comments. Section 4 also
>    miscounted AnonymousUserController as separate from the "6 controllers" — actual active
>    controllers are 4.
> 2. Task section 4 says rate limiting is "ONLY in `AnonymousUserService/Program.cs`."
>    **Wrong — it is ALSO in `OrganizationService/Program.cs:32-64`** (identical policies).
>    (The pre-seeded grep line in section 6 was right; the prose in section 4 was wrong.)

**Key fact for rule authors:** the other 9 services do NOT call `AddAuthentication`/
`AddJwtBearer`, so `[Authorize]` in them would have no effect (no auth handler registered).
`LoggerService` POST is explicitly `[AllowAnonymous]` (`LoggerService/Controllers/LoggerController.cs:59`).

---

## 5. Error shape — `error` vs `message` key mismatch

| Layer | Shape | Evidence |
|---|---|---|
| Controller catch blocks | `return BadRequest(new { error = ex.Message })` / `NotFound(new { error ...})` — **52 occurrences / 19 controllers** | `ProblemService/Controllers/ProblemController.cs:78`; `LoggerService/Controllers/LoggerController.cs:71` (adds `detail`) |
| Global handler | `app.UseExceptionHandler(...)` → 500 with `new { message = error.Error.Message }` | `ProblemService/Program.cs:70-85` |

**Inconsistency to encode:** caught errors use key `error`; the uncaught global handler uses
key `message`. Frontend cannot rely on one key. Rate-limiter rejection also uses `error`
(`AnonymousUserService/Program.cs:60`). `LoggerController` BadRequest additionally adds a
`detail` field (`:71`) — a third variation.

---

## 6. DB / ORM

| Aspect | Pattern | Evidence |
|---|---|---|
| ORM / provider | EF Core + SQL Server, one `DbContext` + one DB per service | `ProblemService/Program.cs:9-10` (`AddDbContext<ProblemContext>...UseSqlServer`) |
| Connection-string key | service-specific | `GetConnectionString("ProblemDB")` (`ProblemService/Program.cs:10`), `"AnonymousUserDB"` (`AnonymousUserService/Program.cs:14`) |
| Startup migration | `db.Database.Migrate()`, several guarded by `IsRelational()` → else `EnsureCreated()` | `ProblemService/Program.cs:53-60`, `LoggerService/Program.cs:30-34`, `ProblemBoxService/Program.cs:49-53`; `AnonymousUserService/Program.cs:90-91` skips when env = `Testing` |
| Save idiom | `public bool SaveChanges() => _context.SaveChanges() > 0;` — **20 repository files** | `ProblemService/Data/ProblemRepository.cs:19-22`, `AnonymousUserService/Data/AnonymousUserRepository.cs:26`, +18 more |
| Query → DTO | repos map entity→DTO via manual `IMapper.Map` (often `foreach` loops), never return entities | `ProblemService/Data/ProblemRepository.cs:24-34, 45-55` |
| Validation | argument checks throw `ArgumentException` inside repo methods | `ProblemService/Data/ProblemRepository.cs:59-64, 77-78` |
| Transactions | none observed | — |

---

## 7. Observability

No distributed tracing, no metrics. Observability == the centralized **LoggerService**.

- Controllers fire-and-(soft-)forget `await _loggerClient.TryLogAsync(new LogCreationDTO {...}, Request.Headers["Authorization"], HttpContext.RequestAborted)` on both success and catch paths — **`TryLogAsync` = 106 occurrences across 18 controllers** (verified, matches task). Example: `ProblemService/Controllers/ProblemController.cs:53-62` (success) and `:68-76` (failure).
- Sink: `LoggerService/Controllers/LoggerController.cs` — POST `[AllowAnonymous]` (`:58-59`), GET/search/delete unauthenticated.
- `LogCreationDTO` field set (`LoggerService/Models/DTOs/LogCreationDTO.cs`):

| Field | Type | Default |
|---|---|---|
| `UserId` | `string?` | null |
| `Action` | `string` | `""` (required-ish, e.g. `"CREATE_PROBLEM"`) |
| `EntityName` | `string?` | null |
| `OldValues` | `string?` | null (JSON) |
| `NewValues` | `string?` | null (JSON, set via `JsonSerializer.Serialize(result)`) |
| `IsSuccess` | `bool` | `true` |
| `ServiceName` | `string` | `""` (e.g. `"ProblemService"`) |
| `HttpMethod` | `string?` | null |

---

## 8. Recurring load-bearing patterns (with grep evidence)

| # | Pattern | Evidence (file:line) |
|---|---|---|
| P1 | **Repository pattern** — `I<Entity>Repository` registered `AddScoped` in `Program.cs`; all data access flows through it | `ProblemService/Program.cs:12-14`; `AnonymousUserService/Program.cs:16-17` |
| P2 | **`bool SaveChanges() => _context.SaveChanges() > 0`** in every repo | `ProblemService/Data/ProblemRepository.cs:19-22`; `OrganizationService/Data/UserRepository.cs`; 20 files total |
| P3 | **Entity→DTO via AutoMapper, never return entities**; AutoMapper registered `AddMaps(typeof(Program).Assembly)` | `ProblemService/Program.cs:16`; map loops `ProblemService/Data/ProblemRepository.cs:28-33` |
| P4 | **Controller audit logging** on success+catch via `_loggerClient.TryLogAsync(...)` (106×/18 controllers) | `ProblemService/Controllers/ProblemController.cs:53-62, 68-76` |
| P5 | **Catch → `BadRequest(new { error = ex.Message })`** (52×/19 controllers) | `ProblemService/Controllers/ProblemController.cs:78`; `LoggerService/Controllers/LoggerController.cs:71` |
| P6 | **Global `UseExceptionHandler` → 500 `{ message }`** in `Program.cs` | `ProblemService/Program.cs:70-85` |
| P7 | **Startup migration** `db.Database.Migrate()` (often `IsRelational()`-guarded) in every `Program.cs` | `ProblemService/Program.cs:50-61`; `AnonymousUserService/Program.cs:87-92` |
| P8 | **Kestrel pinned to `ListenAnyIP(8080)`** in every service (Docker contract) | `ProblemService/Program.cs:43-46`; `AnonymousUserService/Program.cs:64-67` |
| P9 | **Named HttpClient + `appsettings.json:"Services"`** for inter-service URLs (16 regs/10 files) | `ProblemService/Program.cs:27-40`; `ProblemService/appsettings.json:12-16` |
| P10 | **`AnonymousDomain.*` copy-duplicated models** (no shared lib) | `namespace AnonymousDomain` in 20 files |

---

## 9. Proposed rule files (6) and guide files (4)

Slugs match `.coordination/PLAN.md`.

### Rules — `.claude/rules/*.md` ("how to write code", WRONG/CORRECT, table-heavy)

| Slug | Scope | Anchored by |
|---|---|---|
| `controllers-and-errors` | `[ApiController]` + `[Route("api/[controller]")]`, ctor-injected repo/mapper/loggerClient, `try/catch → BadRequest(new { error })`, audit-log on both paths. WRONG: returning entities, missing log, mixing `error`/`message` keys. | §5, §7-P4/P5, ProblemController.cs |
| `repositories-and-dtos` | `I<X>Repository` scoped, `SaveChanges()>0`, map entity→DTO, throw `ArgumentException` for bad input. WRONG: returning entities, no mapper, leaking EF types. | §6, §8-P1/P2/P3 |
| `program-bootstrap` | DbContext+`UseSqlServer(GetConnectionString("<Svc>DB"))`, scoped repos, AutoMapper `AddMaps`, `ListenAnyIP(8080)`, startup `Database.Migrate()`, global `UseExceptionHandler`. | §6, §8-P7/P8 |
| `inter-service-calls` | Prefer `Clients/` Style A (factory, async, `System.Text.Json`, forward bearer, timeout, fail-soft). WRONG: `ServiceCalls/` Style B `new HttpClient().Result`/`Newtonsoft`. Register via `AddHttpClient("Name")` + `appsettings:"Services"`. | §3 |
| `auth-jwt` | JWT validation only where `AddJwtBearer` is registered (2 services); `[Authorize]` is a no-op elsewhere; rate-limit `login`/`register` policies; password hashing in repos. WRONG: `[Authorize]` in a service with no auth handler. | §4 |
| `gateway-routing` | Add an `upstream` + `location /api/<Resource>/` block with the CORS/OPTIONS preamble + `proxy_pass http://<upstream>/api/<Resource>/`; container names are the service hosts. | §10 (below), nginx.conf |

### Guides — `.claude/guides/*.md` ("how the system works")

| Slug | Scope | Reads |
|---|---|---|
| `architecture-overview` | 11 services + Nginx gateway + React SPA; DB-per-service; no shared lib; LoggerService as sink; topology + ports. | nginx.conf, docker-compose.yml, this report |
| `request-and-auth-flow` | Browser → gateway (CORS) → service; JWT issued by AnonymousUser/Organization, decoded client-side; most services don't validate; rate-limited login/register; bearer forwarding via `Clients/`. | AnonymousUserService/Program.cs + AnonymousUserRepository.cs, ProblemController.cs |
| `service-catalog` | Per-service table: route prefix(es), upstream name, DB key, JWT? rate-limit? `ServiceCalls`/`Clients`? | nginx.conf, each Program.cs/appsettings.json |
| `cross-service-contracts` | The two REST styles, named-HttpClient + `appsettings:"Services"` URL config, `LogCreationDTO` shape, `AnonymousDomain` duplication gotcha, error-shape mismatch. | LoggerServiceClient.cs, AttachmentService.cs, LogCreationDTO.cs |

---

## 10. Gateway routing facts (for `gateway-routing` rule)

`gateway/nginx.conf`: 11 `upstream` blocks (one per service, host = docker container name,
all `:8080`). ~18 `location /api/<Resource>/` blocks, each with an identical CORS/OPTIONS
preamble (`Access-Control-Allow-Origin "*"`, methods, headers, 204 preflight) then
`proxy_pass http://<upstream>/api/<Resource>/`. Route→upstream highlights:

| Route prefix(es) | Upstream | Service |
|---|---|---|
| `/api/AnonymousUser/`, `/api/BoxAccessLink/` | `anonymous_service` | AnonymousUserService |
| `/api/Organization/`, `/api/User/`, `/api/UserRole/` | `organization_service` | OrganizationService |
| `/api/Problem/`, `/api/ProblemCategory/`, `/api/ProblemComment/` | `problem_service` | ProblemService |
| `/api/ProblemBox/` | `problem_box_service` | ProblemBoxService |
| `/api/Suggestion/`, `/api/SuggestionCategory/`, `/api/SuggestionComment/`, `/api/Vote/` | `suggestion_service` | SuggestionService |
| `/api/SuggestionBox/` | `suggestion_box_service` | SuggestionBoxService |
| `/api/Subscription/`, `/api/SubscriptionPlan/`, `/api/Payment/` | `subscription_service` | SubscriptionService |
| `/api/BillingNotification/` | `billing_notification_service` | BillingNotificationService |
| `/api/SystemNotification/` | `system_notification_service` | SystemNotificationService |
| `/api/Attachment/` | `attachment_service` | AttachmentService |
| `/api/Logger/` | `logger_service` | LoggerService |
| `/health` | (inline `return 200`) | gateway self |

CORS is handled at the gateway (`add_header` per location), NOT in service code.

---

## STOP — human-review gate

This report is task 001, the human-review gate. Tasks 002–011 should NOT fan out until a
human reviews it. The corrected counts (service `.cs` files, `[Authorize]` 13/4 not 15/6,
rate limiting in 2 services not 1) supersede the task file's pre-seeded numbers.
