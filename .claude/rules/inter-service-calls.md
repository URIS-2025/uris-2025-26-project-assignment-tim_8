# Rule: Inter-Service HTTP Calls

> Cross-link: see `.claude/rules/controllers-and-errors.md` for the `TryLogAsync` call site
> (how controllers invoke `LoggerServiceClient`).

---

## Quick Reference

| | CORRECT (`Clients/` style) | WRONG (`ServiceCalls/` style) |
|---|---|---|
| Client source | `IHttpClientFactory.CreateClient("Name")` | `new HttpClient()` per request |
| Execution | `await SendAsync(...)` | `.GetAsync().Result` / `.ReadAsStringAsync().Result` |
| Bearer forwarding | `req.Headers.Authorization = AuthenticationHeaderValue.Parse(bearerHeader)` | nothing |
| Serializer | `System.Text.Json` (`JsonContent.Create`, `JsonSerializerDefaults.Web`) | `Newtonsoft.Json.JsonConvert` |
| Timeout | Linked `CancellationTokenSource(800 ms)` | none |
| Error handling | swallows silently (fail-soft) | returns `null` on non-2xx |
| Reference file | `ProblemService/Clients/LoggerServiceClient.cs` | `ProblemService/ServiceCalls/AttachmentService.cs` |

---

## CORRECT Pattern — `Clients/LoggerServiceClient.cs`

Reference implementation: `ProblemService/Clients/LoggerServiceClient.cs` (copied verbatim
to all 10 non-logger services — **do not diverge from this template**).

```csharp
// ProblemService/Clients/LoggerServiceClient.cs (full file, 37 lines)
using System.Net.Http.Headers;
using System.Text.Json;

namespace ProblemService.Clients
{
    public class LoggerServiceClient
    {
        private readonly HttpClient _http;

        public LoggerServiceClient() { }                         // ← needed for test mocks

        public LoggerServiceClient(IHttpClientFactory factory)   // ← INJECT factory, not HttpClient
        {
            _http = factory.CreateClient("LoggerService");       // ← named client (line 14)
        }

        public async Task TryLogAsync(
            LogCreationDTO dto, string? bearerHeader, CancellationToken requestCt)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "/api/logger");

                if (!string.IsNullOrWhiteSpace(bearerHeader))
                    req.Headers.Authorization =
                        AuthenticationHeaderValue.Parse(bearerHeader); // ← forward bearer (line 24)

                var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                req.Content = JsonContent.Create(dto, options: opts); // ← STJ, not Newtonsoft (line 27)

                using var timeoutCts =
                    new CancellationTokenSource(TimeSpan.FromMilliseconds(800)); // ← 800 ms cap (line 29)
                using var linked =
                    CancellationTokenSource.CreateLinkedTokenSource(requestCt, timeoutCts.Token); // ← linked (line 30)

                using var res = await _http.SendAsync(req, linked.Token); // ← truly async (line 32)
            }
            catch { }  // ← fail-soft: logging must never crash the caller (line 34)
        }
    }
}
```

### Why each choice matters

| Choice | Reason |
|---|---|
| `IHttpClientFactory` | Manages `HttpMessageHandler` lifetime; prevents socket exhaustion from creating too many `HttpClient` instances. |
| `factory.CreateClient("Name")` | Returns a pre-configured client (base address set in `Program.cs`). |
| `await SendAsync(...)` | Never blocks a thread-pool thread. `.Result` deadlocks under ASP.NET sync context. |
| `AuthenticationHeaderValue.Parse(bearerHeader)` | Propagates the original caller's JWT to downstream services so they can log the correct `UserId`. Without this the log entry has no identity. |
| `CancellationTokenSource.CreateLinkedTokenSource` | Both the request abort *and* the 800 ms timeout can cancel; neither is ignored. |
| `System.Text.Json` | Consistent with the rest of the codebase; no extra NuGet dependency. |
| `catch { }` | Audit logging is observability, not a business requirement. A logger outage must not fail a user request. |

---

## WRONG Pattern — `ServiceCalls/AttachmentService.cs`

Reference anti-pattern: `ProblemService/ServiceCalls/AttachmentService.cs`.

```csharp
// ProblemService/ServiceCalls/AttachmentService.cs (lines 15-28)
public IEnumerable<AttachmentVO> GetAttachmentsByProblemId(Guid problemId)
{
    using (HttpClient client = new HttpClient())                              // WRONG: line 17
    {
        Uri url = new Uri(
            $"{_configuration["Services:AttachmentService"]}api/attachment/problem/{problemId}");
        var response = client.GetAsync(url).Result;                           // WRONG: line 20
        if (!response.IsSuccessStatusCode)
        {
            return null;                                                      // WRONG: line 23
        }
        var content = response.Content.ReadAsStringAsync().Result;            // WRONG: line 25
        return JsonConvert.DeserializeObject<IEnumerable<AttachmentVO>>(content); // WRONG: line 26
    }
}
```

### Why each is wrong

| Anti-pattern | File:Line | Why it is wrong |
|---|---|---|
| `new HttpClient()` per call | `ProblemService/ServiceCalls/AttachmentService.cs:17`, `ProblemBoxService/ServiceCalls/ProblemService.cs:18` | Each `new HttpClient()` opens a new OS socket. Under load the OS runs out of ephemeral ports (socket exhaustion / `TIME_WAIT`). |
| `.GetAsync(url).Result` | `ProblemService/ServiceCalls/AttachmentService.cs:20`, `ProblemBoxService/ServiceCalls/ProblemService.cs:21` | Blocks a thread-pool thread waiting for I/O. Under ASP.NET's synchronization context this can deadlock the entire request pipeline. |
| `.ReadAsStringAsync().Result` | `ProblemService/ServiceCalls/AttachmentService.cs:25`, `ProblemBoxService/ServiceCalls/ProblemService.cs:26` | Same deadlock risk as above. |
| No bearer forwarding | `ProblemService/ServiceCalls/AttachmentService.cs` (no `Authorization` header set) | The downstream service receives no identity. If that service ever enables `[Authorize]`, all calls silently fail. Log entries from the downstream service have no `UserId`. |
| `JsonConvert.DeserializeObject` | `ProblemService/ServiceCalls/AttachmentService.cs:1,26`, `ProblemBoxService/ServiceCalls/ProblemService.cs:1,27` | Uses Newtonsoft.Json, inconsistent with the `Clients/` style and `System.Text.Json` used everywhere else. Adds an unnecessary transitive dependency. |
| `return null` on non-2xx | `ProblemService/ServiceCalls/AttachmentService.cs:23`, `ProblemBoxService/ServiceCalls/ProblemService.cs:24` | Silent failure; the controller receives a `null` and either crashes with a NullReferenceException or returns an empty result with no indication of why. |

---

## `ServiceCalls/` Directories That Contain the Anti-Pattern

These 5 services host the problematic `ServiceCalls/` code (11 files total):

| Service | File | Anti-pattern present |
|---|---|---|
| `ProblemService` | `ServiceCalls/AttachmentService.cs` | `new HttpClient()` + `.Result` + Newtonsoft |
| `ProblemService` | `ServiceCalls/ProblemCommentAuthorUserService.cs` | verify individually |
| `ProblemBoxService` | `ServiceCalls/ProblemService.cs` | `new HttpClient()` + `.Result` + Newtonsoft |
| `SuggestionService` | `ServiceCalls/UserServiceCall.cs` | bare `HttpClient` field + `null` return (no factory-named client — uses `_configuration["ServiceUrls:OrganizationService"]` wrong key) |
| `SuggestionBoxService` | `ServiceCalls/OrganizationServiceCall.cs` | uses factory but no bearer forwarding |
| `SubscriptionService` | `ServiceCalls/OrganizationServiceCall.cs` | uses factory but no bearer forwarding, throws on non-2xx |
| `SubscriptionService` | `ServiceCalls/BillingServiceCall.cs` | uses factory but no bearer forwarding, throws on non-2xx |

> The `SuggestionBoxService` and `SubscriptionService` `ServiceCalls/` files have been
> partially migrated (they use `IHttpClientFactory`) but still lack bearer forwarding and
> match the WRONG interface signature. They are still anti-patterns relative to the full
> `Clients/` style.

---

## Registration — `Program.cs` + `appsettings.json`

### Named HttpClient registration in `Program.cs`

Always register via `AddHttpClient` with the name matching the string passed to
`factory.CreateClient("Name")`. 16 registrations exist across 10 `Program.cs` files.

```csharp
// CORRECT (ProblemService/Program.cs:27-40)
builder.Services.AddHttpClient("LoggerService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:LoggerServiceBaseUrl"]!);
});

builder.Services.AddHttpClient("AttachmentService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:AttachmentService"]);
});

builder.Services.AddHttpClient("OrganizationService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:OrganizationService"]);
});
```

Also register the typed client wrapper as scoped:
```csharp
builder.Services.AddScoped<ProblemService.Clients.LoggerServiceClient>(); // ProblemService/Program.cs:24
```

### `appsettings.json` — `"Services"` block

```json
// ProblemService/appsettings.json:12-16
"Services": {
  "AttachmentService":    "http://attachment-service:8080/",
  "UserService":          "http://organization-service:8080/",
  "LoggerServiceBaseUrl": "http://logger-service"
}
```

Note the trailing slash on some values (`http://attachment-service:8080/`) but not on
`LoggerService` (`http://logger-service`). The client code uses relative paths like
`/api/logger`, so a trailing slash on the base address matters — always include it or
consistently omit it. The `LoggerServiceClient` hard-codes the path as `/api/logger` (line 21),
which works because `HttpClient` resolves relative URIs against the base address.

---

## GOTCHA: Inconsistent Config Key Names

The `"Services"` block key names are **not uniform** across services. This means copy-paste
of a registration block will silently read `null` if the wrong key is used.

| Service | Config key used in `Program.cs` | Actual key in `appsettings.json` |
|---|---|---|
| All services (LoggerService) | `Services:LoggerServiceBaseUrl` | `"LoggerServiceBaseUrl"` |
| `ProblemService` | `Services:AttachmentService` | `"AttachmentService"` (bare service name) |
| `ProblemService` | `Services:OrganizationService` | `"UserService"` (MISMATCH — key is `UserService`, not `OrganizationService`) |
| `ProblemBoxService` | `Services:ProblemService` | `"ProblemService"` |
| `SuggestionService` | `ServiceUrls:OrganizationService` | — missing: `appsettings.json` uses `"Services"` block, not `"ServiceUrls"` |
| `SuggestionBoxService` | `Services:OrganizationService` | `"OrganizationService"` |
| `SubscriptionService` | `Services:OrganizationService` | `"OrganizationService"` |
| `SubscriptionService` | `Services:BillingNotificationService` | `"BillingNotificationService"` |

Notable mismatches:
- `ProblemService/Program.cs:37` registers `AddHttpClient("OrganizationService", ...)` reading
  `Services:OrganizationService`, but `ProblemService/appsettings.json:14` has key
  `"UserService"` pointing to `http://organization-service:8080/` — the value is correct but
  the key does not match. The `AddHttpClient` call will get `null` and throw on startup in
  Docker when this client is actually used. (`ProblemService/appsettings.json:14`,
  `ProblemService/Program.cs:38`)
- `SuggestionService/ServiceCalls/UserServiceCall.cs:19` reads
  `_configuration["ServiceUrls:OrganizationService"]`, but
  `SuggestionService/appsettings.json:10` has the key under `"Services"`, not
  `"ServiceUrls"` — this call always returns `null` at runtime.
- `ProblemBoxService/appsettings.json` has a **duplicate `"Services"` key** (lines 9 and 16);
  in JSON the second one wins, so `"ProblemService"` is silently discarded and only
  `"LoggerServiceBaseUrl"` survives.

**Rule:** when adding a new inter-service URL, add the key under `"Services"` in
`appsettings.json` AND use the exact same string (after `Services:`) in
`builder.Configuration["Services:<key>"]` in `Program.cs`.

---

## `LoggerServiceClient` Copies — 10 Services

`LoggerServiceClient.cs` is copy-duplicated once per non-logger service. All copies should
be byte-for-byte identical to the reference at `ProblemService/Clients/LoggerServiceClient.cs`.
Do not introduce variations.

| Copy | Path |
|---|---|
| 1 | `AnonymousUserService/Clients/LoggerServiceClient.cs` |
| 2 | `AttachmentService/Clients/LoggerServiceClient.cs` |
| 3 | `BillingNotificationService/Clients/LoggerServiceClient.cs` |
| 4 | `OrganizationService/Clients/LoggerServiceClient.cs` |
| 5 | `ProblemBoxService/Clients/LoggerServiceClient.cs` |
| 6 | `ProblemService/Clients/LoggerServiceClient.cs` |
| 7 | `SubscriptionService/Clients/LoggerServiceClient.cs` |
| 8 | `SuggestionBoxService/Clients/LoggerServiceClient.cs` |
| 9 | `SuggestionService/Clients/LoggerServiceClient.cs` |
| 10 | `SystemNotificationService/Clients/LoggerServiceClient.cs` |

There is no shared csproj — changes must be applied to all 10 copies manually.

---

## Adding a New Inter-Service Client — Checklist

1. **Create `<ServiceName>Client.cs`** in `YourService/Clients/` following the
   `LoggerServiceClient` template: inject `IHttpClientFactory`, `CreateClient("Name")`,
   forward `bearerHeader`, linked `CancellationTokenSource`, `await SendAsync`, `catch { }`.
2. **Register the named client** in `YourService/Program.cs`:
   ```csharp
   builder.Services.AddHttpClient("TargetService", client =>
   {
       client.BaseAddress = new Uri(builder.Configuration["Services:TargetServiceBaseUrl"]!);
   });
   builder.Services.AddScoped<YourService.Clients.TargetServiceClient>();
   ```
3. **Add the URL key** to `appsettings.json` under `"Services"` with a trailing slash:
   ```json
   "Services": {
     "TargetServiceBaseUrl": "http://target-service:8080/"
   }
   ```
4. **Inject the typed client** in controllers via constructor injection:
   ```csharp
   private readonly TargetServiceClient _targetClient;
   ```
5. **Do NOT** add to `ServiceCalls/`. If you find a `ServiceCalls/` file in a PR, request
   migration to the `Clients/` pattern.

---

## Call-Site Pattern in Controllers

For `LoggerServiceClient`, the call site in every controller looks like:

```csharp
// ProblemService/Controllers/ProblemController.cs:53-62 (success path)
await _loggerClient.TryLogAsync(new LogCreationDTO
{
    UserId      = ...,
    Action      = "CREATE_PROBLEM",
    EntityName  = "Problem",
    NewValues   = JsonSerializer.Serialize(result),
    IsSuccess   = true,
    ServiceName = "ProblemService",
    HttpMethod  = "POST"
}, Request.Headers["Authorization"], HttpContext.RequestAborted);
```

See `.claude/rules/controllers-and-errors.md` for the full call-site rules (both success and
catch paths, required DTO fields, `IsSuccess` toggling).
