# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Anonymous Reporting System — a microservices platform for organizations to collect anonymous problems and suggestions. 11 ASP.NET Core 8 backend services sit behind an Nginx API gateway, with a React 19 SPA frontend.

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
npm test
npm run build
```

## Architecture

### Backend Services (each has its own SQL Server database)
| Service | Responsibility |
|---------|---------------|
| AnonymousUserService | Anonymous auth, JWT tokens, box access links |
| OrganizationService | Organizations, users, roles |
| ProblemService | Problem submissions and comments |
| ProblemBoxService | Problem box configuration |
| SuggestionService | Suggestions, voting, comments |
| SuggestionBoxService | Suggestion box configuration |
| SubscriptionService | Plans and payments |
| BillingNotificationService | Billing notifications |
| SystemNotificationService | System notifications |
| AttachmentService | File uploads |
| LoggerService | Centralized logging (all other services report here) |

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
Backend test projects use xUnit + Moq + `EF InMemory` for unit tests. Integration tests use `WebApplicationFactory` and may require Docker running. Test projects are co-located in the solution as `<ServiceName>Tests/`.
