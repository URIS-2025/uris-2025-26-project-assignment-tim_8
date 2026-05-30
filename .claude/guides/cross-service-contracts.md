# Cross-Service Contracts

How services talk to each other: who calls whom, via which mechanism, and why the lack of a
shared library makes every cross-service type a maintenance landmine.

> Related rule: [inter-service-calls.md](../rules/inter-service-calls.md) — covers the coding
> how-to. This guide is the map and contract reference.

---

## 1. Call-graph table

| Caller | Callee | Mechanism | Named client | `appsettings` "Services" key | Auth forwarded? |
|---|---|---|---|---|---|
| AnonymousUserService | LoggerService | `Clients/` Style A | `LoggerService` | `Services:LoggerServiceBaseUrl` | yes (bearer header) |
| AttachmentService | LoggerService | `Clients/` Style A | `LoggerService` | `Services:LoggerServiceBaseUrl` | yes |
| BillingNotificationService | LoggerService | `Clients/` Style A | `LoggerService` | `Services:LoggerServiceBaseUrl` | yes |
| OrganizationService | LoggerService | `Clients/` Style A | `LoggerService` | `Services:LoggerServiceBaseUrl` | yes |
| SystemNotificationService | LoggerService | `Clients/` Style A | `LoggerService` | `Services:LoggerServiceBaseUrl` | yes |
| ProblemService | LoggerService | `Clients/` Style A | `LoggerService` | `Services:LoggerServiceBaseUrl` | yes |
| ProblemService | AttachmentService | `ServiceCalls/` Style B | _(raw `new HttpClient()`)_ | `Services:AttachmentService` | **no** |
| ProblemService | OrganizationService (user) | `ServiceCalls/` Style B | _(raw `new HttpClient()`)_ | `Services:UserService` | **no** |
| ProblemBoxService | LoggerService | `Clients/` Style A | `LoggerService` | `Services:LoggerServiceBaseUrl` | yes |
| ProblemBoxService | ProblemService | `ServiceCalls/` Style B | `ProblemService` (registered but unused by impl) | `Services:ProblemService` | **no** |
| SuggestionService | LoggerService | `Clients/` Style A | `LoggerService` | `Services:LoggerServiceBaseUrl` | yes |
| SuggestionService | OrganizationService (user) | `ServiceCalls/` Style B (typed client) | _(typed `HttpClient<IUserServiceCall>`)_ | `ServiceUrls:OrganizationService`* | **no** |
| SuggestionBoxService | LoggerService | `Clients/` Style A | `LoggerService` | `Services:LoggerServiceBaseUrl` | yes |
| SuggestionBoxService | OrganizationService | `ServiceCalls/` Style B (factory-backed) | `OrganizationService` | `Services:OrganizationService` | **no** |
| SubscriptionService | LoggerService | `Clients/` Style A | `LoggerService` | `Services:LoggerServiceBaseUrl` | yes |
| SubscriptionService | OrganizationService | `ServiceCalls/` Style B (factory-backed) | `OrganizationService` | `Services:OrganizationService` | **no** |
| SubscriptionService | BillingNotificationService | `ServiceCalls/` Style B (factory-backed) | `BillingNotificationService` | `Services:BillingNotificationService` | **no** |

> **\* Key-name inconsistency:** `SuggestionService/ServiceCalls/UserServiceCall.cs` reads from
> `_configuration["ServiceUrls:OrganizationService"]` (note `ServiceUrls`, not `Services`), but
> `SuggestionService/appsettings.json` only defines `Services:OrganizationService`. At runtime
> in Docker this lookup returns `null` and every call silently fails.

---

## 2. Two REST styles — quick reference

| Trait | Style A — `Clients/` (correct) | Style B — `ServiceCalls/` (anti-pattern) |
|---|---|---|
| Client source | `IHttpClientFactory.CreateClient("Name")` | `new HttpClient()` (resource leak) |
| Async | fully async (`await SendAsync`) | sync-over-async (`.Result`) — deadlock risk |
| Bearer forwarded | yes — `req.Headers.Authorization = ...` | **no** |
| Serializer | `System.Text.Json` (`JsonContent.Create`, Web defaults) | `Newtonsoft.Json` (`JsonConvert.DeserializeObject`) |
| Error handling | fail-soft: `catch { }` swallows all | returns `null` on non-2xx |
| 800 ms timeout | yes (`CancellationTokenSource(800 ms)`) | none |
| Reference | `ProblemService/Clients/LoggerServiceClient.cs` | `ProblemService/ServiceCalls/AttachmentService.cs` |

All new inter-service calls must follow Style A.

---

## 3. LoggerService contract

### Endpoint

```
POST /api/logger         [AllowAnonymous]
```

`LoggerService/Controllers/LoggerController.cs:58-73`

Response: `201 Created` with the persisted `LogDTO`; `400 Bad Request` with
`{ error, detail }` on failure.

### `LogCreationDTO` fields

All 10 services that call LoggerService carry an **identical local copy** of this DTO in their
own `Clients/` folder. The canonical definition lives in
`LoggerService/Models/DTOs/LogCreationDTO.cs`.

| Field | Type | Required? | Typical value |
|---|---|---|---|
| `UserId` | `string?` | no | JWT subject, e.g. `"d3f...a1"` |
| `Action` | `string` | yes (default `""`) | `"CREATE_PROBLEM"`, `"DELETE_USER"` |
| `EntityName` | `string?` | no | `"Problem"`, `"User"` |
| `OldValues` | `string?` | no | JSON-serialized pre-update entity |
| `NewValues` | `string?` | no | JSON-serialized result entity |
| `IsSuccess` | `bool` | yes (default `true`) | `false` in catch blocks |
| `ServiceName` | `string` | yes (default `""`) | `"ProblemService"` |
| `HttpMethod` | `string?` | no | `"POST"`, `"DELETE"` |

### Fire-and-forget pattern

Every controller calls `TryLogAsync` on both the success and error paths:

```csharp
await _loggerClient.TryLogAsync(new LogCreationDTO {
    Action = "CREATE_PROBLEM", ServiceName = "ProblemService",
    IsSuccess = true, NewValues = JsonSerializer.Serialize(result)
}, Request.Headers["Authorization"], HttpContext.RequestAborted);
```

`TryLogAsync` wraps the POST in `try { } catch { }` with an 800 ms timeout — logging failures
are silently swallowed and never affect the primary response.

Verified 106 occurrences of `TryLogAsync` across 18 controllers
(source: `SCAN_REPORT.md §7`).

---

## 4. Value-object contracts (consumer-side DTOs)

Each of these classes lives inside the **consuming** service, not the producing service. They
are hand-maintained projections of what the producer currently returns. No code-gen, no shared
assembly.

| VO class | Defined in | Producer service | Fields |
|---|---|---|---|
| `AttachmentVO` | `ProblemService/Models/DTOs/AttachmentVO.cs` | AttachmentService | `Id`, `FileName`, `FileType`, `Url`, `UploadedAt` |
| `ProblemCommentAuthorUserVO` | `ProblemService/Models/DTOs/ProblemCommentAuthorUserVO.cs` | OrganizationService | `Id`, `Username` |
| `ProblemVO` | `ProblemBoxService/Models/DTOs/ProblemVO.cs` | ProblemService | `Id`, `Title`, `Description`, `Status`, `Priority`, `CreatedAt` |
| `UserDTO` | `SuggestionService/Models/DTOs/UserDTO.cs` | OrganizationService | `Id`, `Name`, `Surname`, `Email`, `Username` |
| `OrganizationDTO` | `SubscriptionService/Models/ExternalDTOs/OrganizationDTO.cs` | OrganizationService | `Id`, `Name` |
| `OrganizationDTO` | `SuggestionBoxService/Models/ExternalDTOs/OrganizationDTO.cs` | OrganizationService | `Id`, `Name` |
| `BillingNotificationCreateDTO` | `SubscriptionService/Models/ExternalDTOs/BillingNotificationCreateDTO.cs` | BillingNotificationService | `Text`, `OrganizationId`, `PaymentId`, `Type` |

> **GOTCHA — silent deserialization mismatch**
>
> If the producing service renames or removes a field, the consuming service's `JsonConvert.DeserializeObject`
> (Style B) or `JsonSerializer.Deserialize` (Style A) will silently set the field to `null`/default — no
> compile error, no runtime exception, just corrupt data flowing through. There is no schema
> validation, no contract test, and no shared type that would surface the mismatch at build time.
>
> Example: if `AttachmentService` renames `FileName` → `Name`, `ProblemService.AttachmentVO.FileName`
> becomes `null` for every attachment — undetected until a user sees a blank file name.
>
> **Mitigation until a shared library is introduced:** add integration/contract tests that call the
> real producer endpoint and assert the consumer VO deserializes without null fields.

---

## 5. Duplicated `AnonymousDomain` models — THE central contract risk

> **CRITICAL GOTCHA — no shared library**
>
> The namespace `AnonymousDomain.Models.*` appears in **20 source files across 8 services**.
> It looks like a shared library but is physically copy-pasted — there is no `AnonymousDomain.csproj`
> or NuGet package. Each service owns its own copy of every model class.
>
> **Consequence:** changing a domain model (e.g. adding a field to `Suggestion`, renaming a property
> on `User`) requires locating and editing every copy across all affected services. Miss one copy and
> that service silently diverges — EF migrations, JSON serialization, and inter-service response
> parsing can all produce different shapes from different services.
>
> **Files affected (20 total):**

| Service | Files using `namespace AnonymousDomain` |
|---|---|
| AnonymousUserService | `Models/AnonymousUser/AnonymousUser.cs`, `Models/AnonymousUser/AnonymousRefreshToken.cs`, `Models/AnonymousUser/BoxAccessLink.cs` |
| OrganizationService | `Models/User.cs`, `Models/UserRole.cs`, `Models/Organization.cs`, `Models/RefreshToken.cs` |
| SuggestionService | `Models/Suggestion.cs`, `Models/SuggestionComment.cs`, `Models/SuggestionCategory.cs`, `Models/SuggestionCategories.cs`, `Models/Vote.cs`, `Models/BoxAccessLink.cs`, `Models/Attachment.cs`, `Models/AnonymousUser.cs`, `Models/DTOs/SuggestionCommentUpdateDTO.cs`, `Enums/ProblemSuggestionStatus.cs` |
| AttachmentService | `Models/Attachment.cs` |
| BillingNotificationService | `Models/BillingNotification.cs` |
| SystemNotificationService | `Models/SystemNotification.cs` |

> **Resolution path:** extract all `AnonymousDomain.*` types into a dedicated `AnonymousDomain`
> class library project, add it as a project reference in every affected `.csproj`, and delete the
> per-service copies. Until then, treat every `AnonymousDomain.*` change as requiring a multi-service
> edit with careful migration ordering.

---

## 6. `LogCreationDTO` duplication

Alongside `AnonymousDomain`, there are **11 copies** of `LogCreationDTO`:

| Location | Namespace |
|---|---|
| `LoggerService/Models/DTOs/LogCreationDTO.cs` | `LoggerService.Models.DTOs` (canonical) |
| `AnonymousUserService/Clients/LogCreationDTO.cs` | `AnonymousUserService.Clients` |
| `AttachmentService/Clients/LogCreationDTO.cs` | `AttachmentService.Clients` |
| `BillingNotificationService/Clients/LogCreationDTO.cs` | `BillingNotificationService.Clients` |
| `OrganizationService/Clients/LogCreationDTO.cs` | `OrganizationService.Clients` |
| `ProblemBoxService/Clients/LogCreationDTO.cs` | `ProblemBoxService.Clients` |
| `ProblemService/Clients/LogCreationDTO.cs` | `ProblemService.Clients` |
| `SubscriptionService/Clients/LogCreationDTO.cs` | `SubscriptionService.Clients` |
| `SuggestionBoxService/Clients/LogCreationDTO.cs` | `SuggestionBoxService.Clients` |
| `SuggestionService/Clients/LogCreationDTO.cs` | `SuggestionService.Clients` |
| `SystemNotificationService/Clients/LogCreationDTO.cs` | `SystemNotificationService.Clients` |

Because all 10 caller-side copies currently match the canonical definition exactly, field-level
drift has not yet occurred — but any future addition of a field to the canonical DTO must be
replicated in all 10 caller copies manually.

---

## 7. `"Services"` config reference per service

All Docker container-to-container URLs use `http://<container-name>:8080/` (Kestrel is pinned
to 8080 in every service). Note the inconsistencies highlighted below.

| Service | Key | Docker value | Notes |
|---|---|---|---|
| **AnonymousUserService** | `Services:LoggerServiceBaseUrl` | `http://logger-service` | **Missing `:8080`** — relies on Docker DNS default port |
| **AttachmentService** | `Services:LoggerServiceBaseUrl` | `http://logger-service` | Missing `:8080` |
| **BillingNotificationService** | `Services:LoggerServiceBaseUrl` | `http://logger-service` | Missing `:8080` |
| **BillingNotificationService** | `Services:OrganizationService` | `http://organization-service:8080/` | — |
| **OrganizationService** | `Services:LoggerServiceBaseUrl` | `http://logger-service:8080` | Has `:8080` (inconsistent with others) |
| **ProblemBoxService** | `Services:LoggerServiceBaseUrl` | `http://logger-service` | Missing `:8080`; **duplicate `"Services"` key in appsettings.json** (second block overwrites first, so `Services:ProblemService` is unreachable) |
| **ProblemBoxService** | `Services:ProblemService` | `http://problem-service:8080` | Overwritten by duplicate JSON key — effectively missing |
| **ProblemService** | `Services:LoggerServiceBaseUrl` | `http://logger-service` | Missing `:8080` |
| **ProblemService** | `Services:AttachmentService` | `http://attachment-service:8080/` | — |
| **ProblemService** | `Services:UserService` | `http://organization-service:8080/` | Key name `UserService` differs from `OrganizationService` used by other services |
| **SuggestionBoxService** | `Services:LoggerServiceBaseUrl` | `http://logger-service` | Missing `:8080` |
| **SuggestionBoxService** | `Services:OrganizationService` | `http://organization-service:8080/` | — |
| **SuggestionService** | `Services:LoggerServiceBaseUrl` | `http://logger-service` | Missing `:8080` |
| **SuggestionService** | `Services:OrganizationService` | `http://organization-service:8080/` | — |
| **SuggestionService** | _(code reads)_ `ServiceUrls:OrganizationService` | _(key missing)_ | `UserServiceCall.cs` reads `ServiceUrls:OrganizationService` but appsettings only has `Services:OrganizationService` — **always null at runtime** |
| **SubscriptionService** | `Services:LoggerServiceBaseUrl` | `http://logger-service` | Missing `:8080` |
| **SubscriptionService** | `Services:OrganizationService` | `http://organization-service:8080/` | — |
| **SubscriptionService** | `Services:BillingNotificationService` | `http://billing-notification-service:8080/` | — |
| **SystemNotificationService** | `Services:LoggerServiceBaseUrl` | `http://logger-service` | Missing `:8080` |

**Key-name inconsistencies to know:**
- The same OrganizationService target is keyed as `Services:OrganizationService` in most services
  but as `Services:UserService` in ProblemService.
- `SuggestionService/ServiceCalls/UserServiceCall.cs` reads `ServiceUrls:OrganizationService`
  (different root section) — the key is absent from `appsettings.json`, so the call is broken.
- `LoggerServiceBaseUrl` omits `:8080` in 9 of 10 callers; `OrganizationService/appsettings.json`
  includes it. In Docker, plain `http://logger-service` resolves because port 80 is not actually
  used — the named HttpClient's `BaseAddress` is `http://logger-service` and the request path
  `/api/logger` produces `http://logger-service/api/logger`, which Docker routes to 8080 only if
  the gateway or host-network config normalizes it. Verify in production.

---

## Sources

| File | Purpose |
|---|---|
| `.coordination/SCAN_REPORT.md` | Authoritative inventory; verified counts |
| `.coordination/tasks/011.in-progress.guide-cross-service-contracts.md` | Task spec |
| `ProblemService/Clients/LoggerServiceClient.cs` | Style A reference implementation |
| `ProblemService/Clients/LogCreationDTO.cs` | Consumer-side DTO copy |
| `ProblemService/ServiceCalls/AttachmentService.cs` | Style B reference implementation |
| `ProblemService/ServiceCalls/ProblemCommentAuthorUserService.cs` | Style B, user lookup |
| `ProblemService/Models/DTOs/AttachmentVO.cs` | Consumer VO for AttachmentService responses |
| `ProblemService/Models/DTOs/ProblemCommentAuthorUserVO.cs` | Consumer VO for OrganizationService responses |
| `ProblemService/Program.cs` | `AddHttpClient` registrations |
| `ProblemService/appsettings.json` | `"Services"` keys |
| `ProblemBoxService/Program.cs` | `AddHttpClient` registrations |
| `ProblemBoxService/appsettings.json` | `"Services"` keys (duplicate-key bug) |
| `ProblemBoxService/ServiceCalls/ProblemService.cs` | Style B call to ProblemService |
| `ProblemBoxService/Models/DTOs/ProblemVO.cs` | Consumer VO for ProblemService responses |
| `SuggestionService/Program.cs` | `AddHttpClient` registrations |
| `SuggestionService/appsettings.json` | `"Services"` keys |
| `SuggestionService/ServiceCalls/UserServiceCall.cs` | Style B (typed client), broken key |
| `SuggestionService/Models/DTOs/UserDTO.cs` | Consumer VO for OrganizationService user |
| `SuggestionBoxService/Program.cs` | `AddHttpClient` registrations |
| `SuggestionBoxService/appsettings.json` | `"Services"` keys |
| `SuggestionBoxService/ServiceCalls/OrganizationServiceCall.cs` | Style B (factory-backed) |
| `SuggestionBoxService/Models/ExternalDTOs/OrganizationDTO.cs` | Consumer VO for OrganizationService |
| `SubscriptionService/Program.cs` | `AddHttpClient` registrations |
| `SubscriptionService/appsettings.json` | `"Services"` keys |
| `SubscriptionService/ServiceCalls/OrganizationServiceCall.cs` | Style B (factory-backed) |
| `SubscriptionService/ServiceCalls/BillingServiceCall.cs` | Style B (factory-backed) |
| `SubscriptionService/Models/ExternalDTOs/OrganizationDTO.cs` | Consumer VO for OrganizationService |
| `SubscriptionService/Models/ExternalDTOs/BillingNotificationCreateDTO.cs` | Producer-side DTO sent to BillingNotificationService |
| `OrganizationService/Program.cs` | `AddHttpClient` registrations |
| `OrganizationService/appsettings.json` | `"Services"` keys |
| `AnonymousUserService/appsettings.json` | `"Services"` keys |
| `AttachmentService/appsettings.json` | `"Services"` keys |
| `BillingNotificationService/appsettings.json` | `"Services"` keys |
| `SystemNotificationService/appsettings.json` | `"Services"` keys |
| `LoggerService/Controllers/LoggerController.cs` | Endpoint definitions, `[AllowAnonymous]` POST |
| `LoggerService/Models/DTOs/LogCreationDTO.cs` | Canonical DTO definition |
| `LoggerService/Models/Log.cs` | Persisted entity |
| `LoggerService/Data/LoggerRepository.cs` | Repository — maps DTO to entity, saves to DB |
