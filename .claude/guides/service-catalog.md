# Service Catalog

"Where do I change X?" lookup. One subsection per service. All routes follow `[Route("api/[controller]")]`.
Cross-cutting contracts (error shapes, `LogCreationDTO`, inter-service calls) → see [`cross-service-contracts.md`](cross-service-contracts.md).

---

## 1. AnonymousUserService

**Nginx upstream:** `anonymous_service`  **DB:** `AnonymousUserDB`  **JWT issued:** yes  **Rate-limited:** yes (login 5/min, register 3/hr)

### Controllers

| Controller | Route prefix | Key actions |
|---|---|---|
| `AnonymousUserController` | `api/AnonymousUser` | `GET /` [Authorize], `GET /{id}` [Authorize], `POST /` [register RL] → create anon user, `POST /login` [login RL] → returns `{accessToken,refreshToken}`, `POST /refresh` → rotate refresh token, `DELETE /{id}` [Authorize] |
| `BoxAccessLinkController` | `api/BoxAccessLink` | `GET /` (all links), `GET /{id}` — read-only; `//[Authorize]` is commented out |

### Key entities (`Models/AnonymousUser/`)

| Class | Purpose |
|---|---|
| `AnonymousUser` | Anonymous submitter account (namespace `AnonymousDomain.Models.AnonymousUser`) |
| `BoxAccessLink` | Token linking an anonymous user to a problem/suggestion box |
| `AnonymousRefreshToken` | Refresh token record for anonymous JWT rotation |

### Repositories (`Data/`)

| Interface | Implementation |
|---|---|
| `IAnonymousUserRepository` | `AnonymousUserRepository` — password hash, JWT+refresh issuance |
| `IBoxAccessLinkRepository` | `BoxAccessLinkRepository` |

---

## 2. OrganizationService

**Nginx upstream:** `organization_service`  **DB:** `OrganizationDB`  **JWT issued:** yes  **Rate-limited:** yes (login 5/min, register 3/hr)

### Controllers

| Controller | Route prefix | Key actions |
|---|---|---|
| `OrganizationController` | `api/Organization` | `GET /`, `GET /{id}`, `POST /` [Authorize], `PUT /` [Authorize], `DELETE /{id}` [Authorize] |
| `UserController` | `api/User` | `GET /` [Authorize], `GET /{id}` [Authorize], `POST /` [register RL] → create org user, `PUT /` [Authorize], `DELETE /{id}` [Authorize], `POST /login` [login RL] → returns `{accessToken,refreshToken}`, `POST /refresh` → rotate refresh token |
| `UserRoleController` | `api/UserRole` | `GET /`, `GET /{id}`, `POST /` [Authorize], `PUT /` [Authorize], `DELETE /{id}` [Authorize] |

### Key entities (`Models/`)

| Class | Purpose |
|---|---|
| `Organization` | Org record (namespace `AnonymousDomain.Models.Organization`) |
| `User` | Org user with hashed password (namespace `AnonymousDomain.Models.Organization`) |
| `UserRole` | Role assigned to a user (namespace `AnonymousDomain.Models.Organization`) |
| `RefreshToken` | Refresh token record for org user JWT rotation |

### Repositories (`Data/`)

| Interface | Implementation |
|---|---|
| `IOrganizationRepository` | `OrganizationRepository` |
| `IUserRepository` | `UserRepository` — password hash, JWT+refresh issuance |
| `IUserRoleRepository` | `UserRoleRepository` |

---

## 3. ProblemService

**Nginx upstream:** `problem_service`  **DB:** `ProblemDB`  **JWT:** none (no `AddJwtBearer`)  **ServiceCalls:** none; uses `Clients/` only

### Controllers

| Controller | Route prefix | Key actions |
|---|---|---|
| `ProblemController` | `api/Problem` | `GET /`, `GET /{id}`, `GET /problembox/{problemBoxId}`, `POST /` → create, `PUT /` → update, `DELETE /{id}` |
| `ProblemCategoryController` | `api/ProblemCategory` | `GET /`, `GET /{id}`, `POST /`, `PUT /`, `DELETE /{id}` |
| `ProblemCommentController` | `api/ProblemComment` | `GET /`, `GET /{id}`, `POST /`, `PUT /`, `DELETE /{id}` |

### Key entities (`Models/`)

| Class | Purpose |
|---|---|
| `Problem` | Problem submission |
| `ProblemCategory` | Category for grouping problems |
| `ProblemComment` | Comment on a problem |

### Repositories (`Data/`)

| Interface | Implementation |
|---|---|
| `IProblemRepository` | `ProblemRepository` |
| `IProblemCategoryRepository` | `ProblemCategoryRepository` |
| `IProblemCommentRepository` | `ProblemCommentRepository` |

---

## 4. ProblemBoxService

**Nginx upstream:** `problem_box_service`  **DB:** `ProblemBoxDB`  **JWT:** none  **ServiceCalls:** has `ServiceCalls/` (Style B anti-pattern)

### Controllers

| Controller | Route prefix | Key actions |
|---|---|---|
| `ProblemBoxController` | `api/ProblemBox` | `GET /`, `GET /{id}`, `GET /organization/{id}`, `GET /boxaccesslink/{boxAccessLinkId}`, `POST /`, `PUT /`, `DELETE /{id}` |

### Key entities (`Models/`)

| Class | Purpose |
|---|---|
| `ProblemBox` | Problem box configuration tied to an organization |

### Repositories (`Data/`)

| Interface | Implementation |
|---|---|
| `IProblemBoxRepository` | `ProblemBoxRepository` |

---

## 5. SuggestionService

**Nginx upstream:** `suggestion_service`  **DB:** `SuggestionDB`  **JWT:** none  **ServiceCalls:** has `ServiceCalls/` (Style B anti-pattern)

> **GOTCHA — duplicated AnonymousDomain models:** `SuggestionService/Models/` contains local copies of `AnonymousUser.cs`, `BoxAccessLink.cs`, and `Attachment.cs` in the `AnonymousDomain` namespace. These are **not authoritative** — editing them does NOT propagate to other services. The canonical sources are `AnonymousUserService/Models/AnonymousUser/` and `AttachmentService/Models/`. See also §Duplicates section below.

### Controllers

| Controller | Route prefix | Key actions |
|---|---|---|
| `SuggestionController` | `api/Suggestion` | `GET /`, `GET /{id}`, `GET /user/{id}`, `POST /`, `PUT /`, `DELETE /{id}` |
| `SuggestionCategoryController` | `api/SuggestionCategory` | `GET /`, `GET /{id}`, `POST /`, `PUT /`, `DELETE /{id}` |
| `SuggestionCommentController` | `api/SuggestionComment` | `GET /`, `GET /{id}`, `POST /`, `PUT /`, `DELETE /{id}` |
| `VoteController` | `api/Vote` | `GET /` (stub — returns empty list), `GET /suggestion/{id}`, `POST /`, `DELETE /{id}` |

### Key entities (`Models/` — own domain)

| Class | Purpose |
|---|---|
| `Suggestion` | Suggestion submission |
| `SuggestionCategory` | Category for grouping suggestions |
| `SuggestionCategories` | Join/mapping entity (category ↔ suggestion) |
| `SuggestionComment` | Comment on a suggestion |
| `Vote` | Up/down vote on a suggestion |

### Duplicated cross-service models (in `SuggestionService/Models/`)

| Class | Namespace | Authoritative source |
|---|---|---|
| `AnonymousUser.cs` | `AnonymousDomain` | `AnonymousUserService/Models/AnonymousUser/AnonymousUser.cs` |
| `BoxAccessLink.cs` | `AnonymousDomain` | `AnonymousUserService/Models/AnonymousUser/BoxAccessLink.cs` |
| `Attachment.cs` | `AnonymousDomain` | `AttachmentService/Models/Attachment.cs` |

### Repositories (`Data/`)

| Interface | Implementation |
|---|---|
| `ISuggestionRepository` | `SuggestionRepository` |
| `ISuggestionCategoryRepository` | `SuggestionCategoryRepository` |
| `ISuggestionCommentRepository` | `SuggestionCommentRepository` |
| `IVoteRepository` | `VoteRepository` |

---

## 6. SuggestionBoxService

**Nginx upstream:** `suggestion_box_service`  **DB:** `SuggestionBoxDB`  **JWT:** none  **ServiceCalls:** has `ServiceCalls/` (Style B anti-pattern)

### Controllers

| Controller | Route prefix | Key actions |
|---|---|---|
| `SuggestionBoxController` | `api/SuggestionBox` | `GET /`, `GET /{id}`, `GET /organization/{organizationId}`, `POST /`, `PUT /`, `DELETE /{id}`, `DELETE /organization/{organizationId}` |

### Key entities (`Models/`)

| Class | Purpose |
|---|---|
| `SuggestionBox` | Suggestion box configuration tied to an organization |

### Repositories (`Data/`)

| Interface | Implementation |
|---|---|
| `ISuggestionBoxRepository` | `SuggestionBoxRepository` |

---

## 7. SubscriptionService

**Nginx upstream:** `subscription_service`  **DB:** `SubscriptionDB`  **JWT:** none  **ServiceCalls:** has `ServiceCalls/` (Style B anti-pattern; calls `BillingNotificationService` via `BillingServiceCall`)

### Controllers

| Controller | Route prefix | Key actions |
|---|---|---|
| `SubscriptionController` | `api/Subscription` | `GET /`, `GET /{id}`, `POST /` → create + fires `BillingServiceCall.CreateBillingNotificationAsync`, `PUT /`, `DELETE /{id}` |
| `SubscriptionPlanController` | `api/SubscriptionPlan` | `GET /`, `GET /{id}`, `POST /` → create plan (no logger client wired) |
| `PaymentController` | `api/Payment` | `GET /`, `GET /{id}`, `GET /bySubscription/{subscriptionId}`, `POST /`, `PUT /`, `DELETE /{id}` |

### Key entities (`Models/`)

| Class | Purpose |
|---|---|
| `Subscription` | Active subscription for an org |
| `SubscriptionPlan` | Plan definition (tiers/pricing) |
| `Payment` | Payment record linked to a subscription |

### Repositories (`Data/`)

| Interface | Implementation |
|---|---|
| `ISubscriptionRepository` | `SubscriptionRepository` |
| `ISubscriptionPlanRepository` | `SubscriptionPlanRepository` |
| `IPaymentRepository` | `PaymentRepository` |

---

## 8. BillingNotificationService

**Nginx upstream:** `billing_notification_service`  **DB:** `BillingNotificationDB`  **JWT:** none  **Note:** `//[Authorize]` commented out on controller

### Controllers

| Controller | Route prefix | Key actions |
|---|---|---|
| `BillingNotificationController` | `api/BillingNotification` | `GET /`, `GET /{id}`, `POST /`, `PUT /`, `DELETE /{id}` — `[Authorize]` is commented out |

### Key entities (`Models/`)

| Class | Purpose |
|---|---|
| `BillingNotification` | Billing event notification (namespace `AnonymousDomain.Models.BillingNotification`) |

### Repositories (`Data/`)

| Interface | Implementation |
|---|---|
| `IBillingNotificationRepository` | `BillingNotificationRepository` |

---

## 9. SystemNotificationService

**Nginx upstream:** `system_notification_service`  **DB:** `SystemNotificationDB`  **JWT:** none

### Controllers

| Controller | Route prefix | Key actions |
|---|---|---|
| `SystemNotificationController` | `api/SystemNotification` | `GET /` (list all), `POST /` → create, `DELETE /{id}` — no update endpoint |

### Key entities (`Models/`)

| Class | Purpose |
|---|---|
| `SystemNotification` | System-level notification (namespace `AnonymousDomain.Models.SystemNotification`) |

### Repositories (`Data/`)

| Interface | Implementation |
|---|---|
| `ISystemNotificationRepository` | `SystemNotificationRepository` |

---

## 10. AttachmentService

**Nginx upstream:** `attachment_service`  **DB:** `AttachmentDB`  **JWT:** none

### Controllers

| Controller | Route prefix | Key actions |
|---|---|---|
| `AttachmentController` | `api/Attachment` | `GET /` (all), `GET /suggestion/{id}`, `GET /problem/{id}`, `POST /`, `PUT /`, `DELETE /{id}` |

### Key entities (`Models/`)

| Class | Purpose |
|---|---|
| `Attachment` | File attachment record (namespace `AnonymousDomain`; **authoritative copy**) |

### Repositories (`Data/`)

| Interface | Implementation |
|---|---|
| `IAttachmentRepository` | `AttachmentRepository` |

---

## 11. LoggerService

**Nginx upstream:** `logger_service`  **DB:** `LoggerDB`  **JWT:** none — POST is explicitly `[AllowAnonymous]`

### Controllers

| Controller | Route prefix | Key actions |
|---|---|---|
| `LoggerController` | `api/Logger` | `GET /[?take=100]`, `HEAD /`, `GET /{id}`, `GET /search?userId&action&entityName&serviceName&httpMethod&isSuccess&fromUtc&toUtc&take`, `POST /` [AllowAnonymous] → create log entry, `DELETE /{id}`, `OPTIONS /` [AllowAnonymous] |

### Key entities (`Models/`)

| Class | Purpose |
|---|---|
| `Log` | Audit log record persisted by the sink |

### Repositories (`Data/`)

| Interface | Implementation |
|---|---|
| `ILoggerRepository` | `LoggerRepository` |

---

## Cross-service AnonymousDomain Duplication Summary

`grep -rl "namespace AnonymousDomain"` returns **20 files** (excluding task/scan docs). Services holding duplicated copies:

| Service | Duplicated files in `Models/` |
|---|---|
| **SuggestionService** | `AnonymousUser.cs`, `BoxAccessLink.cs`, `Attachment.cs`, `Suggestion.cs`, `SuggestionComment.cs`, `SuggestionCategory.cs`, `SuggestionCategories.cs`, `Vote.cs` |
| **BillingNotificationService** | `BillingNotification.cs` |
| **SystemNotificationService** | `SystemNotification.cs` |
| **AttachmentService** | `Attachment.cs` (authoritative) |
| **AnonymousUserService** | `AnonymousUser.cs`, `BoxAccessLink.cs`, `AnonymousRefreshToken.cs` (authoritative) |
| **OrganizationService** | `Organization.cs`, `User.cs`, `UserRole.cs`, `RefreshToken.cs` (authoritative) |

**Rule:** Editing one service's `AnonymousDomain.*` class does NOT propagate. There is no shared project — each service owns its physical copy. Always edit the service-local copy, and note which service is authoritative.

---

## Sources

Dirs/files inspected:

- `AnonymousUserService/Controllers/` — all 2 controllers read
- `AnonymousUserService/Models/AnonymousUser/` — 3 entity files
- `AnonymousUserService/Data/` — 4 files
- `OrganizationService/Controllers/` — all 3 controllers read
- `OrganizationService/Models/` — 4 entity files
- `OrganizationService/Data/` — 6 files
- `ProblemService/Controllers/` — all 3 controllers read
- `ProblemService/Models/` — 3 entity files (top-level)
- `ProblemService/Data/` — 6 files
- `ProblemBoxService/Controllers/` — 1 controller read
- `ProblemBoxService/Models/` — 1 entity file
- `ProblemBoxService/Data/` — 2 files
- `SuggestionService/Controllers/` — all 4 controllers read
- `SuggestionService/Models/` — 8 entity files (incl. duplicated AnonymousDomain models)
- `SuggestionService/Data/` — 8 files
- `SuggestionBoxService/Controllers/` — 1 controller read
- `SuggestionBoxService/Models/` — 1 entity file
- `SuggestionBoxService/Data/` — 2 files
- `SubscriptionService/Controllers/` — all 3 controllers read
- `SubscriptionService/Models/` — 3 entity files
- `SubscriptionService/Data/` — 6 files
- `BillingNotificationService/Controllers/` — 1 controller read
- `BillingNotificationService/Models/` — 1 entity file
- `BillingNotificationService/Data/` — 2 files
- `SystemNotificationService/Controllers/` — 1 controller read
- `SystemNotificationService/Models/` — 1 entity file
- `SystemNotificationService/Data/` — 2 files
- `AttachmentService/Controllers/` — 1 controller read
- `AttachmentService/Models/` — 1 entity file
- `AttachmentService/Data/` — 2 files
- `LoggerService/Controllers/` — 1 controller read
- `LoggerService/Models/` — 1 entity file (`Log.cs`)
- `LoggerService/Data/` — 2 files
- `.coordination/SCAN_REPORT.md` — authoritative counts and namespace grep results
- `grep -rl "namespace AnonymousDomain"` → 24 raw matches (20 source files excl. task/scan docs)
