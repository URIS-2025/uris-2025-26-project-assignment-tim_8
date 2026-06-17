# Architecture Overview — Anonymous Reporting System

One-screen mental model. Read this first. For deeper detail see the cross-linked rules below.

---

## Service Inventory

| Service | Container name | DB name | Exposed? | Purpose |
|---|---|---|---|---|
| AnonymousUserService | `anonymous-user-service` | `AnonymousUserDB` | gateway only | Anonymous auth, JWT + refresh-token issuance, box-access links. **Issues JWTs.** |
| OrganizationService | `organization-service` | `OrganizationDb` | gateway only | Organizations, users, roles. **Issues JWTs.** |
| ProblemService | `problem-service` | `ProblemDB` | gateway only | Problem submissions, comments, categories |
| ProblemBoxService | `problem-box-service` | `ProblemBoxDB` | gateway only | Problem-box configuration |
| SuggestionService | `suggestion-service` | `SuggestionDB` | gateway only | Suggestions, votes, comments, categories |
| SuggestionBoxService | `suggestion-box-service` | `SuggestionBoxDB` | gateway only | Suggestion-box configuration |
| SubscriptionService | `subscription-service` | `SubscriptionDB` | gateway only | Subscription plans and payments |
| BillingNotificationService | `billing-notification-service` | `BillingNotificationDB` | gateway only | Billing notifications |
| SystemNotificationService | `system-notification-service` | `SystemNotificationDB` | gateway only | System-level notifications |
| AttachmentService | `attachment-service` | `AttachmentDB` | gateway only | File uploads and attachments |
| LoggerService | `logger-service` | `LoggerDB` | gateway only | Centralized audit-log sink — all other services POST here |
| Nginx gateway | `api-gateway` | — | **port 80** | Routes `/api/*` to services; handles CORS |
| SQL Server | `sql-server` | (host) | **port 1433** | Single SQL Server instance; each service has its own DB |

All 11 application services listen on `:8080` (internal Docker network only, via `expose`). Only the gateway (`:80`) and SQL Server (`:1433`) publish host ports.

---

## Deployment Topology

```
Browser / React SPA (:3000 dev)
        |
        | HTTP  (dev: direct to gateway)
        v
  Nginx api-gateway  (:80 published)
        |
        | proxy_pass  http://<container-name>:8080
        |
  ┌─────┴──────────────────────────────────────────────────────┐
  │  anonymous-user-service:8080                               │
  │  organization-service:8080                                 │
  │  problem-service:8080                                      │
  │  problem-box-service:8080                                  │
  │  suggestion-service:8080                                   │
  │  suggestion-box-service:8080                               │
  │  subscription-service:8080                                 │
  │  billing-notification-service:8080                         │
  │  system-notification-service:8080                          │
  │  attachment-service:8080                                   │
  │  logger-service:8080                                       │
  └─────┬──────────────────────────────────────────────────────┘
        |
        | EF Core / SQL Server provider
        v
  sql-server:1433  (published to host)
```

The React SPA (`:3000` dev server) points all API calls to `http://localhost/api/…`, which is the gateway. The gateway must be running even in local dev.

---

## Gateway Route Map

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
| `/health` | (inline `return 200`) | gateway self-check |

CORS (`Access-Control-Allow-Origin "*"`) is handled entirely in `gateway/nginx.conf` — no service adds CORS headers.

See `.claude/rules/gateway-routing.md` for how to add a new route.

---

## Deployment

```bash
docker-compose up -d    # builds all 11 service images + starts gateway + sql-server
docker-compose down     # stop
docker-compose logs -f problem-service   # tail one service
```

Services resolve each other by **Docker container name** (e.g. `http://logger-service:8080`). These names are baked into `appsettings.json` under `"Services"` and overridden by compose `environment` entries in `docker-compose.yml`.

### Where configuration comes from

| Config key | Source (local dev) | Source (Docker) |
|---|---|---|
| `ConnectionStrings:<SvcDB>` | `appsettings.json` (`Server=localhost\\SQLEXPRESS;...;Trusted_Connection=True`) | compose `environment` (`Server=sql-server;User Id=sa;Password=tim8urisPassword!`) |
| `Services:<Name>BaseUrl` / `Services:<Name>` | `appsettings.json` (container names — works only if on Docker network) | compose `environment` (same values, explicit) |
| `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience` | not in appsettings (must be set manually) | compose `environment` (only for AnonymousUserService and OrganizationService) |
| `ASPNETCORE_ENVIRONMENT` | `Properties/launchSettings.json` | compose `environment: Development` |

The `appsettings.Development.json` for ProblemService only adjusts `Logging` and a `Services` block inside `Logging` — it does not override `ConnectionStrings` or `Services` top-level keys. Local development requires a running SQL Server instance (default: `localhost\\SQLEXPRESS`) and the Docker gateway stack for inter-service calls.

See `.claude/rules/program-bootstrap.md` for the full `Program.cs` startup sequence.

---

## Standard Per-Service Folder Layout

Every service (observed on ProblemService, consistent across the codebase):

```
<ServiceName>/
  Controllers/        # [ApiController] [Route("api/[controller]")] classes
  Models/             # EF entities + DTOs (namespace AnonymousDomain.Models.* duplicated — see Gotchas)
  Data/               # I<Entity>Repository interfaces + concrete implementations
  Context/            # EF DbContext (one per service)
  Migrations/         # EF Code-First migration files
  Clients/            # Style-A HttpClient wrappers (factory, async, System.Text.Json, bearer-forwarding)
  ServiceCalls/       # Style-B anti-pattern clients (new HttpClient(), sync .Result, Newtonsoft.Json) — present in 5 services
  Profiles/           # AutoMapper Profile classes (entity → DTO mappings)
  Enums/              # Shared enum definitions (not all services)
  Program.cs          # DI registration, middleware pipeline, Kestrel :8080
  appsettings.json    # Connection string (local), Services URLs
  Dockerfile          # Multi-stage: sdk:8.0 build → aspnet:8.0 runtime; EXPOSE 8080
  <Service>.csproj    # TFM net8.0; key packages: EF Core 8, AutoMapper 16, Swashbuckle 6
```

See `.claude/rules/inter-service-calls.md` for when to use `Clients/` vs avoiding `ServiceCalls/`.

---

## Top-Level Repository Layout

```
/
  AnonymousApp.sln             # Solution file referencing all service + test projects
  CLAUDE.md                    # Project guidance for Claude Code
  docker-compose.yml           # All services + gateway + sql-server
  gateway/
    nginx.conf                 # Upstreams + location blocks; CORS headers
  <ServiceName>/               # 11 service projects (see per-service layout above)
  <ServiceName>Tests/          # xUnit unit-test projects (naming: see Gotchas)
  <ServiceName>IntegrationTests/  # WebApplicationFactory integration tests (some services)
  Frontend/
    reporting-app/             # React 19 SPA (npm start → :3000)
      src/
        pages/                 # Full-page components
        components/            # Shared UI components
        services/              # One file per backend resource
        context/AuthContext    # JWT storage + login/logout + role-based access
  Backup/                      # Snapshot copies (ignore)
```

---

## Gotchas

| # | Gotcha | Impact |
|---|---|---|
| G1 | **No shared project.** `namespace AnonymousDomain.Models.*` model classes are physically copied across 20 files in 8 services. Editing one copy does NOT propagate. | Treat `AnonymousDomain.*` as "convention, not contract." Each service owns its copy. |
| G2 | **JWT validation in only 2 services.** Only `AnonymousUserService` and `OrganizationService` call `AddAuthentication().AddJwtBearer(...)`. The other 9 services do NOT register an auth handler — `[Authorize]` on them would be a no-op. `LoggerService` POST is explicitly `[AllowAnonymous]`. | Never add `[Authorize]` to a service without also wiring up `AddJwtBearer` in its `Program.cs`. |
| G3 | **`Database.Migrate()` runs on every startup.** Each service calls `db.Database.Migrate()` (some guarded by `IsRelational()`, else `EnsureCreated()`) in `Program.cs`. Cold-start requires SQL Server to be reachable. | If the DB is unavailable at startup the service crashes. Start `sql-server` first (compose `depends_on` handles this in Docker). |
| G4 | **Test-project naming is inconsistent.** A single service may have up to three test directories: `<Svc>Tests/`, `<Svc>Test/` (no "s"), and `<Svc>IntegrationTests/`. Some names are also misspelled (e.g. `LogerServiceIntegrationTests` — missing "g"). | `dotnet test AnonymousApp.sln` runs all of them; individual `--filter` commands must use the actual project name. |
| G5 | **Two inter-service client styles coexist.** `Clients/` uses the correct pattern (IHttpClientFactory, async, System.Text.Json, bearer-forwarding, 800 ms timeout). `ServiceCalls/` is an anti-pattern (`new HttpClient()`, `.Result` sync-over-async, Newtonsoft.Json, no bearer). New code must follow `Clients/` style. | See `.claude/rules/inter-service-calls.md`. |
| G6 | **Error-response key mismatch.** Controller `catch` blocks return `{ error: "..." }` (52 occurrences). The global `UseExceptionHandler` returns `{ message: "..." }`. Frontend cannot rely on a single key. | Do not unify silently — changing one shape is a breaking change for callers already consuming the other. |

---

## Cross-Links

- `.claude/rules/program-bootstrap.md` — full `Program.cs` DI + middleware + startup migration pattern
- `.claude/rules/inter-service-calls.md` — `Clients/` Style A vs `ServiceCalls/` Style B; how to register and configure
- `.claude/rules/gateway-routing.md` — how to add a new upstream + location block in `nginx.conf`

---

## Sources

Files read to produce this guide (repo-relative paths):

| File | Purpose |
|---|---|
| `.coordination/SCAN_REPORT.md` | Authoritative service inventory, file counts, patterns, gotchas |
| `.coordination/tasks/008.in-progress.guide-architecture-overview.md` | Task spec |
| `docker-compose.yml` | Container names, published ports, environment variables, depends_on |
| `gateway/nginx.conf` | Upstream definitions, location/route blocks, CORS headers |
| `CLAUDE.md` | Existing project overview |
| `ProblemService/appsettings.json` | Local connection string and Services URLs format |
| `ProblemService/appsettings.Development.json` | Dev environment overrides |
| `ProblemService/ProblemService.csproj` | TFM (net8.0), package references |
| `ProblemService/Dockerfile` | Multi-stage build/runtime image pattern |
