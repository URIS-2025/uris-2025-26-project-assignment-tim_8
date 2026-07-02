# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Anonymous Reporting System — a microservices platform for organizations to collect anonymous problems and suggestions. 11 ASP.NET Core 8 backend services sit behind an Nginx API gateway, with a React 19 SPA frontend.

## Knowledge Base

Read these before writing backend code. Rules = "how to write code here" (WRONG/CORRECT pairs); guides = "how the system works". All are evidence-backed against this repo.

### Rules (`.claude/rules/`)
| File | Scope |
|------|-------|
| `controllers-and-errors.md` | Controller shape: `[ApiController]`/`[Route("api/[controller]")]`, `ActionResult<T>`, try/catch + `TryLogAsync` audit on both paths, `new { error }` response, exception→status mapping |
| `repositories-and-dtos.md` | Repository pattern (`IXxxRepository`, `SaveChanges() > 0`), manual `IMapper` mapping, validate-and-throw inside repos, DTO/VO naming (`CreationDTO`/`CreatedDTO`/`UpdateDTO`/`DTO`/`VO`) |
| `program-bootstrap.md` | `Program.cs` wiring order: DbContext + service-specific conn key, AutoMapper, Kestrel `ListenAnyIP(8080)`, `Database.Migrate()` on startup, `partial class Program` |
| `inter-service-calls.md` | CORRECT `Clients/` (IHttpClientFactory + bearer forward + timeout) vs WRONG `ServiceCalls/` (`new HttpClient()` + `.Result`); named clients + `appsettings` `Services` keys |
| `auth-jwt.md` | JWT bearer (only AnonymousUserService + OrganizationService), `[Authorize]` (13 across 4 controllers), rate-limit policies, token issuance + hashing live in repos |
| `gateway-routing.md` | Adding an nginx `location /api/<Controller>/` block: OPTIONS preflight, duplicated CORS preamble, container-name `:8080` upstream |
| `service-to-service-calls.md` | Non-fatal client pattern for log/notify/lookup calls; JWT forwarding; mockable clients |
| `frontend-testing.md` | CRA-5/Jest can't resolve react-router-dom v7 — test below the router; running single suites |
| `spa-auth-session.md` | AuthContext rehydration race (`initializing` guard, not sessionStorage); bearer on `[Authorize]` fetches |
| `ef-migrations.md` | Dedupe + repoint FKs before a unique index; empty-only idempotent seeding |

### Guides (`.claude/guides/`)
| File | Topic |
|------|-------|
| `architecture-overview.md` | Service/container/DB/port inventory, topology, deployment, per-service file map, gotchas |
| `request-and-auth-flow.md` | End-to-end trace of `POST /api/Problem` + JWT issue/refresh/validate + rate limiting |
| `service-catalog.md` | Per-service controllers→routes, key entities, repositories; duplicated-model flags |
| `cross-service-contracts.md` | Who-calls-whom call graph, LoggerService log contract, VO contracts, duplicated `AnonymousDomain` models risk |
| `notifications.md` | How org system/billing notifications are produced, resolved, and authorized |
| `mcp-gateway.md` | McpGateway security boundary (T7): dual-principal authz pipeline, Dapper+scrubbed-views+SELECT-only read layer, gateway-composed org-scope, InvocationContext propagation, audit scrubbing |

The original scan that produced these lives in `.coordination/SCAN_REPORT.md`.

## Commands

### Docker (recommended for full stack)
```bash
docker-compose up -d          # start everything
docker-compose down           # stop
docker-compose logs -f <svc>  # e.g. problem-service
```

### Backend
```bash
dotnet build AnonymousApp.sln
dotnet test AnonymousApp.sln
dotnet test --filter "FullyQualifiedName~ProblemServiceTests.ProblemTests"  # single test class
cd ServiceName && dotnet run
cd ServiceName && dotnet ef migrations add <Name> && dotnet ef database update
```

### Frontend
```bash
cd Frontend/reporting-app
npm install
npm start        # dev server on :3000
npm test         # react-scripts test (Jest, watch mode)
npm run build
```
The dev server runs on `:3000`, but the app makes API calls to `http://localhost/api/*` (the Nginx gateway on `:80`). Backend services are **not** individually exposed — only the gateway (`:80`) and SQL Server (`:1433`) are published. So frontend work that touches the API requires the Docker stack (`docker-compose up -d`) running alongside `npm start`.

## Architecture

### Backend Services (each has its own SQL Server database)
All services are **ASP.NET Core 8 + EF Core + SQL Server**, listen on Kestrel `:8080`, and call LoggerService fire-and-forget for audit logs. The "Calls out to" column lists *additional* inter-service dependencies.

| Service | Responsibility | Calls out to |
|---------|----------------|--------------|
| AnonymousUserService | Anonymous auth, JWT tokens, box access links | Logger |
| OrganizationService | Organizations, users, roles | Logger |
| ProblemService | Problem submissions and comments | Attachment, Organization, Logger |
| ProblemBoxService | Problem box configuration | Problem, Logger |
| SuggestionService | Suggestions, voting, comments | Organization, Logger |
| SuggestionBoxService | Suggestion box configuration | Organization, Logger |
| SubscriptionService | Plans and payments | Organization, BillingNotification, Logger |
| BillingNotificationService | Billing notifications | Logger |
| SystemNotificationService | System notifications | Logger |
| AttachmentService | File uploads | Logger |
| LoggerService | Centralized logging (all other services report here) | — |

Two inter-service styles coexist: `Clients/` (named `IHttpClientFactory`, async, forwards bearer) and the legacy `ServiceCalls/` (`new HttpClient()` + `.Result`, no bearer). See `.claude/rules/inter-service-calls.md`.

Each service follows the same internal layout: `Controllers/`, `Models/`, `Data/` (repository interfaces + implementations), `Context/` (EF DbContext), `Migrations/`, `Clients/` (HttpClient wrappers for inter-service calls), `Profiles/` (AutoMapper).

### API Gateway
`gateway/nginx.conf` routes all `/api/*` traffic to the appropriate service. In Docker, services are resolved by container name (e.g. `http://logger-service:8080`). CORS is handled at the gateway level.

### Frontend (`Frontend/reporting-app/src/`)
- `pages/` — full page components (Admin dashboard, Public portal, Anonymous flows, Billing)
- `components/` — shared UI components
- `services/` — one file per backend resource, calls `http://localhost/api/Resource/`
- `context/AuthContext` — JWT storage, login/logout, role-based access (admin/manager/user)

Key routes: `/` home, `/login` & `/signup` org auth, `/anonymous/*` anonymous flows, `/portal` public browse, `/admin/*` protected dashboard.

### Patterns
- **Repository pattern** — all data access via `IRepository` interfaces registered as scoped in `Program.cs`
- **AutoMapper DTOs** — entities never returned directly; mapped through `Profiles/`
- **JWT auth** — issued by AnonymousUserService; frontend decodes with `jwt-decode` and stores in localStorage via AuthContext
- **Service-to-service calls** — via named `HttpClient` factories; target URLs configured in each service's `appsettings.json` under `Services`
- **DB migrations on startup** — `context.Database.Migrate()` is called in `Program.cs` automatically

### Connection Strings
Docker: `Server=sql-server;Database=<Name>DB;User Id=sa;Password=tim8urisPassword!;TrustServerCertificate=True;`  
Local: `Server=localhost\\TEW_SQLEXPRESS;Database=<Name>DB;Trusted_Connection=True;TrustServerCertificate=True;`

### Testing
Backend test projects use xUnit + Moq + `EF InMemory` for unit tests. Integration tests use `WebApplicationFactory` and may require Docker running.

Test project naming is **inconsistent** — there is no single convention. Suffixes vary across services (`<ServiceName>Test`, `<ServiceName>Tests`, `<ServiceName>IntegrationTests`), and some services have more than one test project (e.g. both `AttachmentServiceTest/` and `AttachmentServiceTests/`). When targeting a specific test, list the directories first rather than assuming the `Tests` suffix. Integration tests generally live in the `*IntegrationTests/` projects.

## Constraints (hard rules specific to this codebase)

- **Kestrel is always `ListenAnyIP(8080)`** — never hardcode a different port; the gateway upstreams assume `:8080`.
- **Keep `public partial class Program { }`** at the end of every `Program.cs` — `WebApplicationFactory` integration tests depend on it.
- **`Database.Migrate()` runs on startup** in every service. Add a migration with `dotnet ef migrations add <Name>`; never hand-edit a generated migration.
- **Connection-string key is service-specific** (`"ProblemDB"`, `"AnonymousUserDB"`, `"OrganizationDb"`, …) — not `DefaultConnection`.
- **Never return an entity from a controller** — always map to a DTO via a `Profiles/` AutoMapper profile.
- **Error shape:** controllers return `return BadRequest/NotFound(new { error = ex.Message })`; the global `UseExceptionHandler` returns `new { message }`. This key mismatch is intentional-by-accident — match the surrounding code, don't "fix" it ad hoc.
- **Audit every controller write** via `_loggerClient.TryLogAsync(...)` on BOTH success and failure paths (fire-and-forget, forwards the bearer header).
- **Every new controller needs a matching `location /api/<Controller>/` block** in `gateway/nginx.conf` (with the full CORS preamble) or it is unreachable from the frontend.
- **Prefer `Clients/` over `ServiceCalls/`** for inter-service calls: `IHttpClientFactory` + bearer forwarding + timeout, never `new HttpClient()` + `.Result`.
- **JWT validation exists only in AnonymousUserService + OrganizationService.** Adding `[Authorize]` to a controller in any other service is a no-op until `AddAuthentication().AddJwtBearer(...)` is wired in that service's `Program.cs`.
- **`AnonymousDomain.*` models are copy-duplicated across ~20 files** with no shared project — changing an entity means editing every copy.
- **Known config bugs to respect when touching inter-service config:** `ProblemBoxService/appsettings.json` has a duplicate `"Services"` JSON key (first block silently dropped); `SuggestionService/ServiceCalls/UserServiceCall.cs` reads a `ServiceUrls:` key that doesn't exist in its `appsettings.json`. See `.claude/guides/cross-service-contracts.md`.
