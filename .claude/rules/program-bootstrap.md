# Rule: Program.cs Bootstrap

Every ASP.NET Core 8 service in this repo follows the same `Program.cs` wiring order.
Deviate from it and things break silently (wrong port, skipped migrations, broken integration
tests). This file is the single checklist for adding a new service or auditing an existing one.

For JWT/rate-limiter wiring that applies only to `AnonymousUserService` and `OrganizationService`,
see `.claude/rules/auth-jwt.md`.

---

## 1. Service-to-DB-key map

Every service has its own `ConnectionStrings` section in `appsettings.json`. The key is
**never** `"DefaultConnection"` — it is always named after the service.

| Service | Connection-string key | Evidence |
|---|---|---|
| AnonymousUserService | `AnonymousUserDB` | `AnonymousUserService/Program.cs:14` |
| OrganizationService | `OrganizationDb` | `OrganizationService/Program.cs:14` |
| ProblemService | `ProblemDB` | `ProblemService/Program.cs:10` |
| ProblemBoxService | `ProblemBoxDB` | `ProblemBoxService/Program.cs:13` |
| SuggestionService | `SuggestionDB` | `SuggestionService/Program.cs:15` |
| SuggestionBoxService | `SuggestionBoxDB` | `SuggestionBoxService/Program.cs:22` |
| SubscriptionService | `SubscriptionDB` | `SubscriptionService/Program.cs:21` |
| BillingNotificationService | `BillingNotificationDB` | `BillingNotificationService/Program.cs:10` |
| SystemNotificationService | `SystemNotificationDB` | `SystemNotificationService/Program.cs:11` |
| AttachmentService | `AttachmentDB` | `AttachmentService/Program.cs:12` |
| LoggerService | `LoggerDB` | `LoggerService/Program.cs:20` |

`appsettings.json` shape (Docker variant vs local-dev variant — both must be present):

```json
{
  "ConnectionStrings": {
    "ProblemDB": "Server=sql-server;Database=ProblemDB;User Id=sa;Password=tim8urisPassword!;TrustServerCertificate=True;"
  },
  "Services": {
    "LoggerServiceBaseUrl": "http://logger-service",
    "AttachmentService":    "http://attachment-service:8080/",
    "OrganizationService":  "http://organization-service:8080/"
  }
}
```

(Evidence: `ProblemService/appsettings.json:17-19`, `ProblemService/appsettings.json:12-16`)

---

## 2. Canonical wiring order — builder phase

```csharp
var builder = WebApplication.CreateBuilder(args);

// ── 1. DbContext ────────────────────────────────────────────────────────────
builder.Services.AddDbContext<XxxContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("XxxDB")));
// Key is service-specific — see table above. NEVER "DefaultConnection".

// ── 2. Scoped repositories ──────────────────────────────────────────────────
builder.Services.AddScoped<IXxxRepository, XxxRepository>();
// One line per repository. All data access must flow through a repo interface.

// ── 3. AutoMapper ───────────────────────────────────────────────────────────
builder.Services.AddAutoMapper(config => config.AddMaps(typeof(Program).Assembly));
// Scans the service's own assembly for all AutoMapper Profile classes.
// SuggestionBoxService uses the shorter AddAutoMapper(typeof(Program).Assembly) — both work.

// ── 4. Controllers + Swagger ────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── 5. Named HttpClients (inter-service) ────────────────────────────────────
builder.Services.AddHttpClient("LoggerService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:LoggerServiceBaseUrl"]!);
});
builder.Services.AddScoped<XxxService.Clients.LoggerServiceClient>();
// Add one AddHttpClient block per downstream service. URL from appsettings "Services" section.

// ── 6. Kestrel port ─────────────────────────────────────────────────────────
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);  // ALL services use 8080 — Docker contract.
});
```

Evidence for each step:

| Step | Evidence (file:line) |
|---|---|
| `AddDbContext` + `GetConnectionString("XxxDB")` | `ProblemService/Program.cs:9-10`, `AnonymousUserService/Program.cs:13-14`, `AttachmentService/Program.cs:11-12` |
| `AddScoped<IRepo, Repo>()` | `ProblemService/Program.cs:12-14`, `AnonymousUserService/Program.cs:16-17`, `SuggestionService/Program.cs:18-21` |
| `AddAutoMapper(config => config.AddMaps(typeof(Program).Assembly))` | `ProblemService/Program.cs:16`, `AnonymousUserService/Program.cs:18`, `AttachmentService/Program.cs:14` |
| `AddControllers()` + `AddEndpointsApiExplorer()` + `AddSwaggerGen()` | `ProblemService/Program.cs:18-21`, `LoggerService/Program.cs:9-12`, `SuggestionService/Program.cs:9-11` |
| `ListenAnyIP(8080)` | `ProblemService/Program.cs:43-46`, `AnonymousUserService/Program.cs:64-67`, `OrganizationService/Program.cs:66-69` |

---

## 3. Startup migration block — TWO guard variants

The migrate block always uses `app.Services.CreateScope()` and runs **before** `app.Run()`.
There are two guard styles in the codebase — choose the one appropriate for the service:

### Variant A — `IsRelational()` / `EnsureCreated()` (3 services)

Used by services whose test suite uses an EF In-Memory provider. In-Memory is non-relational,
so `Migrate()` would throw; `EnsureCreated()` is safe there.

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<XxxContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();    // SQL Server (Docker / local dev)
    }
    else
    {
        db.Database.EnsureCreated();  // In-Memory (unit-test host)
    }
}
```

Evidence: `ProblemService/Program.cs:50-61`, `AttachmentService/Program.cs:24-31`,
`SuggestionBoxService/Program.cs:53-60`.

### Variant B — `IsEnvironment("Testing")` guard (8 services)

Skips migration entirely when the environment name is `"Testing"`. The test host sets this
via `WebApplicationFactory` environment override. Does NOT call `EnsureCreated()` — the test
host is responsible for creating the schema.

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<XxxContext>();
    if (!app.Environment.IsEnvironment("Testing"))
        db.Database.Migrate();
}
```

Evidence: `AnonymousUserService/Program.cs:87-92`, `OrganizationService/Program.cs:89-94`,
`SuggestionService/Program.cs:38-43`, `BillingNotificationService/Program.cs:33-38`,
`SystemNotificationService/Program.cs:37-41`, `SubscriptionService/Program.cs:61-66`,
`LoggerService/Program.cs:24-36`.

**Which variant to use for a new service?**
- If tests use EF In-Memory: use Variant A.
- If tests use `WebApplicationFactory` with a test-specific environment name: use Variant B.
- Either is acceptable; do NOT omit both guards (migration will run inside test hosts and fail).

---

## 4. App pipeline wiring order

```csharp
var app = builder.Build();

// ── Startup migrate (see §3) ────────────────────────────────────────────────
using (var scope = app.Services.CreateScope()) { ... }

// ── Swagger (dev only) ──────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ── Global exception handler ────────────────────────────────────────────────
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var error = context.Features.Get<IExceptionHandlerFeature>();
        if (error != null)
            await context.Response.WriteAsJsonAsync(new { message = error.Error.Message });
    });
});

// ── (Auth services only) UseRateLimiter → UseAuthentication ─────────────────
// Only AnonymousUserService and OrganizationService call these.
// See .claude/rules/auth-jwt.md for details.

// ── Authorization + routing ─────────────────────────────────────────────────
app.UseAuthorization();
app.MapControllers();
app.Run();

// ── Integration-test hook (REQUIRED — see §5) ───────────────────────────────
public partial class Program { }
```

Evidence for global exception handler: `ProblemService/Program.cs:70-85`,
`LoggerService/Program.cs:45-60`.

**Note on error keys:** the global handler emits `{ message }` (key = `message`);
controller catch-blocks emit `{ error }` (key = `error`). This mismatch is intentional — do
not "fix" one side without updating the frontend client.

---

## 5. `public partial class Program { }` — REQUIRED

Every `Program.cs` ends with this single line:

```csharp
public partial class Program { }
```

It exposes the top-level `Program` type to test assemblies so that
`WebApplicationFactory<Program>` compiles. Omitting it causes all integration tests to fail
with a compile error.

Evidence: all 11 services have it — `ProblemService/Program.cs:93`,
`AnonymousUserService/Program.cs:111`, `OrganizationService/Program.cs:113`,
`LoggerService/Program.cs:70`, `AttachmentService/Program.cs:43`,
`SuggestionService/Program.cs:55`, `SuggestionBoxService/Program.cs:78`,
`BillingNotificationService/Program.cs:53`, `SystemNotificationService/Program.cs:63`,
`SubscriptionService/Program.cs:83`, `ProblemBoxService/Program.cs:87`.

---

## 6. EF migration workflow

| Command | Purpose |
|---|---|
| `dotnet ef migrations add <Name>` | Scaffold a new migration from model changes |
| `dotnet ef database update` | Apply pending migrations to the local DB |
| `dotnet ef migrations remove` | Delete the last (unapplied) migration |

Run from inside the service directory (e.g., `cd ProblemService`). Migration files live in
`<Service>/Migrations/`. At Docker startup the application applies them automatically via the
`Database.Migrate()` call in `Program.cs` — no manual `database update` is needed in Docker.

---

## 7. WRONG vs CORRECT

| Mistake | Why it breaks | Correct form |
|---|---|---|
| Omitting `public partial class Program { }` | `WebApplicationFactory<Program>` cannot reference the type; all integration tests fail to compile | Always end `Program.cs` with `public partial class Program { }` |
| Hardcoding a port other than 8080 (e.g., `ListenAnyIP(5000)`) | Nginx upstream blocks and `docker-compose` health checks all target `:8080`; service is unreachable behind the gateway | `options.ListenAnyIP(8080)` — no exceptions |
| Using `GetConnectionString("DefaultConnection")` | No service has a key named `DefaultConnection`; `UseSqlServer` receives `null` and throws at runtime | Use the service-specific key from the table in §1 |
| `db.Database.EnsureCreated()` on the production / relational path | `EnsureCreated()` does NOT run migrations; schema is created at the initial model state and never updated | Use `db.Database.Migrate()` on the relational path; `EnsureCreated()` only in the `else` branch of `IsRelational()` |
| Skipping the migrate block entirely | DB schema is never applied; service crashes on first DB call | Always include the migrate block after `app.Build()` |
| `AddAutoMapper(Assembly)` without scanning the service assembly | Profiles in the service are not registered; mapping throws `AutoMapperMappingException` at runtime | `AddAutoMapper(config => config.AddMaps(typeof(Program).Assembly))` |

---

## 8. Complete skeleton (new service)

Copy-paste this for a new service, replacing `Xxx` / `XxxDB`:

```csharp
using Microsoft.EntityFrameworkCore;
using XxxService.Context;
using XxxService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<XxxContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("XxxDB")));

builder.Services.AddScoped<IXxxRepository, XxxRepository>();

builder.Services.AddAutoMapper(config => config.AddMaps(typeof(Program).Assembly));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("LoggerService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:LoggerServiceBaseUrl"]!);
});
builder.Services.AddScoped<XxxService.Clients.LoggerServiceClient>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<XxxContext>();
    if (!app.Environment.IsEnvironment("Testing"))
        db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (error != null)
            await context.Response.WriteAsJsonAsync(new { message = error.Error.Message });
    });
});

app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
```

For JWT + rate-limiter wiring (auth-issuing services only), see `.claude/rules/auth-jwt.md`.
