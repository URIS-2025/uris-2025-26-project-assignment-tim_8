# Feature Requirements — Admin → Create-Organization flow fixes

Branch: `task/008-admin-org-flow-fixes` (off `dev`)
Source: refined plan from `/question-me` (3 defects, user-approved scope).

## Summary

Three independent defects in the admin create-organization flow:

1. **Reload logs the user out** — rehydration race, not a storage problem.
2. **Plan dropdown shows 4 plans ("basic" twice)** — duplicate DB row; no seed, no dedup.
3. **Create organization returns 401** — frontend omits the `Authorization` header.

---

## FIX #1 — Stay logged in across reloads

**Root cause.** `AuthProvider` starts with `user = null` and rehydrates asynchronously inside a
`useEffect` (`AuthContext.js:105-138`). `ProtectedRoute` checks `if (!user) return <Navigate to="/login">`
on the *first* render (`App.js:31-32`) — before rehydration completes — so any reload of an
`/admin/*` route bounces to `/login`. The token already persists in `localStorage`; storage is not
the problem.

**Decision.** Keep `localStorage` (survive browser restart — user's choice). Do **not** move to
sessionStorage.

**Changes.**
- `AuthContext.js`: add `initializing` state (default `true`); set it `false` in a `finally` once
  `initializeAuth()` completes (both success and failure paths). Expose `initializing` via context value.
- `App.js`: `ProtectedRoute` reads `initializing`; while `initializing === true` render a lightweight
  loading state (not a redirect); only redirect to `/login` once `initializing === false && !user`.

**Acceptance.**
- Logged-in admin hard-reloads `/admin/dashboard` → stays on dashboard, still authenticated.
- Still logged in after closing and reopening the browser (localStorage retained).
- A genuinely unauthenticated visit to `/admin/*` still redirects to `/login` (after init).

---

## FIX #2 — Exactly three plans (clean data + prevent recurrence)

**Root cause.** `SubscriptionDB.SubscriptionPlans` contains a duplicate "basic" row. There is no
seeding code and no UI dedup; `SubscriptionPlanController.GetAll` (`:27-31`) and the dropdown
(`AdminDashboard.jsx:373-377`) faithfully pass through whatever rows exist.

**Decision.** Clean data + prevent (user's choice): delete the duplicate, add a unique index on
`Title`, and seed exactly 3 canonical plans on a fresh (empty) DB.

**Canonical 3 plans** (chosen to match the frontend price inference at `AdminDashboard.jsx:159-161`):
| Title | Description | Inferred price |
|-------|-------------|---------------|
| Basic | Basic plan for small teams. | €49 |
| Pro | Pro plan for growing organizations. | €199 |
| Premium | Premium plan with all features. | €499 |

**Changes.**
- `SubscriptionContext.cs` `OnModelCreating`: add `entity.HasIndex(sp => sp.Title).IsUnique();` to the
  `SubscriptionPlan` config.
- New EF migration (e.g. `UniqueSubscriptionPlanTitle`): `Up()` first runs raw SQL to delete duplicate
  titles (keep one row per title, ordered by `Id`), **then** creates the unique index. Order matters —
  index creation fails if duplicates remain. `Down()` drops the index.
- Idempotent seeding: extract a `SubscriptionPlanSeeder.Seed(SubscriptionContext)` that adds the 3
  canonical plans **only when the table is empty**, then call it from `Program.cs` after
  `db.Database.Migrate()`, skipped under the `Testing` environment (mirrors the existing Migrate guard).

**Acceptance.**
- Create-org dialog shows exactly 3 plans, no duplicate.
- Fresh/empty DB → seeded with exactly the 3 canonical plans.
- Non-empty DB → seeder makes no changes (existing data untouched).
- Inserting a second row with an existing title is rejected by the unique index.

**Not unit-testable (documented):** the unique index and the dedupe SQL are SQL-Server-specific and are
not exercised by EF InMemory; they are verified by integration/manual run (consistent with the repo's
WebApplicationFactory + Docker integration approach). The **seeder** logic IS unit-tested with EF InMemory.

---

## FIX #3 — Create organization no longer 401s

**Root cause.** `organizationService.create()` (and `update`/`delete`) omit the `Authorization` header
(`organizationService.js:19-29`). The token is issued *and* validated by OrganizationService itself
(`UserRepository.cs:140-166`), so there is **no** cross-service JWT mismatch. `[Authorize]` on create
(`OrganizationController.cs:39`) needs only an authenticated user, which the admin is once the header is sent.

**Changes.**
- `organizationService.js`: add a `getAuthHeader()` helper mirroring `userService.js:3-6`; merge it into
  the `headers` of `create`, `update`, and `delete`.

**Acceptance.**
- Logged-in admin creates an organization → 201 Created (not 401).
- The create request carries `Authorization: Bearer <token>` (reads `authToken` from localStorage).
- `update` and `delete` also send the header.

---

## Out of scope (deferred)

- Restricting org creation to the `admin` role (today: any authenticated user).
- Auditing other frontend services missing auth headers.
- Refresh-token edge cases.

---

## Files to change

| File | Fix |
|------|-----|
| `Frontend/reporting-app/src/context/AuthContext.js` | #1 — `initializing` state |
| `Frontend/reporting-app/src/App.js` | #1 — `ProtectedRoute` init guard |
| `Frontend/reporting-app/src/services/organizationService.js` | #3 — auth header on create/update/delete |
| `SubscriptionService/Context/SubscriptionContext.cs` | #2 — unique index on Title |
| `SubscriptionService/Data/SubscriptionPlanSeeder.cs` (new) | #2 — idempotent seeder |
| `SubscriptionService/Program.cs` | #2 — call seeder after Migrate |
| `SubscriptionService/Migrations/*` (new) | #2 — dedupe + unique index |

## Tests

| Test | Fix | Framework |
|------|-----|-----------|
| `organizationService` attaches `Authorization` on create/update/delete | #3 | Jest (RTL) |
| `ProtectedRoute` shows loading while `initializing`, redirects only after | #1 | Jest (RTL) |
| `SubscriptionPlanSeeder` adds 3 plans when empty; no-ops when populated | #2 | xUnit + EF InMemory |

## Verification (manual, end-to-end)

1. **#1** — login as admin, hard-reload `/admin/dashboard` → stays authenticated; also after browser restart.
2. **#2** — create-org dialog shows exactly 3 plans; drop & recreate DB → still 3; duplicate title insert rejected.
3. **#3** — as logged-in admin, create org → 201 (not 401); request carries `Authorization: Bearer …`; smoke-test update/delete.
