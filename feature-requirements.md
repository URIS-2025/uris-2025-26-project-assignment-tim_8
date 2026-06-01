# Feature Requirements — Auth Input Hardening (valid emails + strong passwords)

Branch: `feature/auth-input-hardening` (off `dev`). Source plan: `AUTH_HARDENING_PLAN.md`.
Scope confirmed: **full-stack**, org + anonymous **signup/login**. No reset, no email verification,
no cookie migration (deferred — Appendix A of the plan).

## Research findings (current state — verified)

| Area | Current state | Gap |
|---|---|---|
| Org email format | `UserCreationDTO [EmailAddress]` + `[ApiController]` auto-400 | none (works) |
| Org password complexity | `UserCreationDTO [RegularExpression]` upper+digit+special, len 8–64 | **missing lowercase** in the regex |
| Anon password complexity | `AnonymousUserCreationDTO` length 8–64 only | **no complexity regex at all** |
| Anon username format | regex `^[a-zA-Z0-9_]+$`, 3–30 | none (works) |
| Breach check (HIBP) | none | **missing everywhere** |
| Frontend enforcement | `Login.jsx`/`AnonymousSignup.jsx` advertise rules, don't enforce | **the main UX gap** |
| Defect: `systemUserService.js:1` | uses `https://` | broken; fix to `http://` |
| Defect: `AnonymousLogin.jsx:20,44` | `alert()` | replace with inline error block |
| Defect: `Login.jsx:110` | `setIsSubmitting(true)` before guard `return`s | button stuck disabled after early return |

**Established pattern:** these two services validate via **DataAnnotations on the DTO** +
`[ApiController]` auto-validation (not repo guards). We extend that pattern, not replace it.

## Shared Contract (identical both sides)

- **Password:** length 8–64; must contain lowercase, uppercase, digit, special char.
  Canonical regex: `^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,64}$`
- **Email (org only):** `^[^\s@]+@[^\s@]+\.[^\s@]+$`, lowercased+trimmed.
- **Username (anon only):** `^[a-zA-Z0-9_]+$`, length 3–30.
- **Breach check:** HIBP k-anonymity (SHA-1, 5-char prefix to `api.pwnedpasswords.com/range/{p}`),
  full hash never sent. Backend = hard reject on signup; frontend = warn+block. **Fail-open** on
  network error (both sides).
- **Canonical messages** (verbatim both sides):
  - `Password must be at least 8 characters.`
  - `Password must be at most 64 characters.`
  - `Password must include uppercase, lowercase, a number, and a special character.`
  - `This password has appeared in a data breach. Please choose another.`
  - `Please enter a valid email address.`
  - `Username may only contain letters, digits, and underscores (3–30 chars).`
  - `Passwords do not match.`
- **Error shape:** controllers/`new { error }`; DataAnnotations auto-400 emits `{ errors }`;
  frontend `extractErrorMessage` already reads both. Don't change shapes.

## Golden rule
Frontend validation is UX only. Backend re-validates independently (DataAnnotations + breach check
in the action). A curl bypass must still be rejected.

---

## BACKEND requirements (must go GREEN before frontend track)

### B1 — Anon password complexity (DataAnnotations)
- Add `[RegularExpression(canonical)]` + the canonical complexity message to
  `AnonymousUserCreationDTO.Password`.
- **Test (RED→GREEN):** `Validator.TryValidateObject` on `AnonymousUserCreationDTO` with
  `"aaaaaaaa"` → invalid (currently valid = RED). `"Abcdef1!"` → valid.

### B2 — Org password complexity includes lowercase
- Update `UserCreationDTO.Password` regex to the canonical (adds `(?=.*[a-z])`).
- **Test (RED→GREEN):** `UserCreationDTO` with `"PASSWORD1!"` (no lowercase) → invalid (currently
  valid = RED). `"Password1!"` → valid. `"bad@"`-format email → invalid (regression).

### B3 — HIBP breach checker (the only real code addition)
- `IPasswordBreachChecker { Task<bool> IsBreachedAsync(string password, CancellationToken ct); }`
- `PwnedPasswordsClient` impl following `.claude/rules/inter-service-calls.md` Clients/ template:
  `IHttpClientFactory.CreateClient("PwnedPasswords")`, async `SendAsync`, linked
  `CancellationTokenSource` timeout, `catch { return false; }` (fail-open). SHA-1 prefix/suffix
  k-anonymity. Copy into BOTH `OrganizationService/Clients/` and `AnonymousUserService/Clients/`
  (no shared project — matches repo convention).
- Register named client + scoped service in both `Program.cs`.
- **Test (RED→GREEN), client unit test with mocked `HttpMessageHandler`:**
  - response contains the password's suffix → `true`
  - response lacks it → `false`
  - handler throws → `false` (fail-open)

### B4 — Wire breach check into both create actions
- `OrganizationService UserController.CreateUser`: inject `IPasswordBreachChecker`; before
  `_userRepository.CreateUser`, `if (await checker.IsBreachedAsync(user.Password, ct))` →
  log `IsSuccess=false` (existing logger pattern) + `return BadRequest(new { error = <breach msg> })`.
- `AnonymousUserService AnonymousUserController.Create`: make it `async Task<ActionResult<...>>`,
  inject the checker, same guard before `CreateUser` → `return BadRequest(new { error = <breach msg> })`.
- **Test (RED→GREEN), controller test with mocked checker returning true:** create → `BadRequest`
  with the canonical breach message; returning false → `Created` (regression).

### Backend test homes (existing projects)
- `OrganizationServiceTests/` (xUnit + Moq + EF InMemory) — DTO validation tests, `UserControllerTests.cs`, new `PwnedPasswordsClientTests.cs`.
- `AnonymousUserServiceTest/` — DTO validation tests, `AnonymousUserControllerTests.cs`, new `PwnedPasswordsClientTests.cs`.
- Integration tests (`*IntegrationTests.cs`) that create users with breached-looking passwords must
  register a **stub `IPasswordBreachChecker` returning false** in the test factory so they don't
  hit live HIBP and don't fail.

### Backend rules to honor
`controllers-and-errors.md` (audit log both paths, `new { error }`), `inter-service-calls.md`
(Clients/ template, fail-soft), `program-bootstrap.md` (registration order, `partial class Program`),
`repositories-and-dtos.md` (DTO annotations are the existing validation home here).

---

## FRONTEND requirements (start only after backend green)

### F1 — Shared validator module `src/utils/validation.js`
- `validateEmail(email) -> {valid, message}`, `validatePassword(pwd) -> {valid, message}`
  (length + canonical complexity), `validateUsername(name) -> {valid, message}`. Canonical messages.
- **Test (RED→GREEN), `src/utils/validation.test.js`:** valid + each invalid branch per function.

### F2 — HIBP soft helper `src/services/pwnedService.js`
- `isPwned(password) -> Promise<boolean>`; SHA-1 via `crypto.subtle`, range request to
  `https://api.pwnedpasswords.com/range/{prefix}` (NOT `API_BASE_URL`). Network/crypto error →
  `false` (fail-open).
- **Test (RED→GREEN):** fetch rejects → resolves `false` (fail-open). (Mock `fetch`/`crypto.subtle`.)

### F3 — Org signup gating (`Login.jsx`)
- In `handleSubmit` signup path: `validateEmail` → `validatePassword` → confirm (if present) →
  `await isPwned` (block + breach message if true) → existing role/org checks → service call.
- **Fix:** move `setIsSubmitting(true)` to AFTER the guard `return`s. Make helper text match the
  real enforced rule. Keep inline `{error && <AlertCircle/>}`.
- Login path: only "all fields required" (do NOT strength-check existing users on login).

### F4 — Anon signup gating (`AnonymousSignup.jsx`)
- Enforce `validatePassword` + `validateUsername` + confirm-match + `await isPwned` as hard gates
  (keep the strength meter for UX).

### F5 — Defects
- `AnonymousLogin.jsx`: replace both `alert()` with `const [error,setError]` + inline error block
  (per `forms-validation.md`).
- `systemUserService.js:1`: `https://` → `http://`.

### Frontend test home
`react-scripts test` (Jest + RTL). `src/utils/validation.test.js`, `src/services/pwnedService.test.js`.
Form-level behavior (submit blocked on invalid) verified manually; one RTL smoke test optional.

### Frontend rules to honor
`services-api.md` (module shape, http not https, throw on !ok), `forms-validation.md` (guard
clauses, inline errors not alert), `data-fetching-state.md`, `auth-context.md`, `styling.md`.

---

## Out of scope (do not drift)
Email verification, password reset/change (the `/forgot-password` link stays dead), httpOnly-cookie
migration (Appendix A), mid-session 401 refresh.

## Verification plan
- Backend: `dotnet test` for both services green (DTO validation, breach client, controller breach wiring); curl a weak/breached/bad-email signup at the gateway → still rejected.
- Frontend: `npm test` green (validators, pwned fail-open); manual — invalid inputs blocked inline with canonical messages, no network call; anon login shows inline errors; submit button not stuck.

## Files to create / modify
**Backend — modify:** `AnonymousUserService/Models/DTOs/AnonymousUser/AnonymousUserCreationDTO.cs`,
`OrganizationService/Models/DTOs/UserCreationDTO.cs`,
`OrganizationService/Controllers/UserController.cs`, `AnonymousUserService/Controllers/AnonymousUserController.cs`,
`OrganizationService/Program.cs`, `AnonymousUserService/Program.cs`.
**Backend — create:** `OrganizationService/Clients/IPasswordBreachChecker.cs` + `PwnedPasswordsClient.cs`,
`AnonymousUserService/Clients/` same two; tests: `PwnedPasswordsClientTests.cs` (both), DTO + controller test additions; stub checker in both integration factories.
**Frontend — create:** `src/utils/validation.js` (+ `.test.js`), `src/services/pwnedService.js` (+ `.test.js`).
**Frontend — modify:** `src/pages/Login.jsx`, `src/pages/AnonymousSignup.jsx`, `src/pages/AnonymousLogin.jsx`, `src/services/systemUserService.js`.
