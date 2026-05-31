# Auth Input-Hardening Plan — Valid Emails + Strong Passwords

> Pre-flight refined plan (from `/question-me`). **Zero code by design.**
> Hand the **Backend** section to backend-Claude, the **Frontend** section to frontend-Claude.
> The **Shared Contract** is the seam — both must implement to it identically.

---

## Problem Statement

Today the SPA *advertises* strong-input rules but does not *enforce* them, and the backend does
not independently guarantee them either:

- `Login.jsx:211-223` shows HTML5 `type="email"` + `minLength={8}` + helper text promising
  "uppercase, digit, special character" — but `handleSubmit` (`:107-161`) never actually checks
  email format or password complexity. A user can register a weak password / malformed email.
- `AnonymousSignup.jsx:7-15` has a password-strength *meter*, but the only hard gate is
  `length < 8` (`:37`). The "strong" regex is cosmetic.
- No shared validator exists — each form re-invents ad-hoc checks, so rules drift.
- Backend org/anon `CreateUser` repos hash the password (BCrypt) but do **not** validate strength
  or email format, so anything that bypasses the SPA (curl/devtools) is accepted.

The real problem: **input validity is not guaranteed at the only layer that matters (the server),
and the client UX that should guide users is inconsistent and partly fake.**

## Chosen Approach

1. **One shared validator on each side**, enforcing the *same* rules with the *same* messages.
2. **Frontend = UX gate** (instant inline feedback, blocks submit). **Backend = real gate**
   (re-validates everything; rejects bypassed requests).
3. **Password policy: complexity + breach check.** 8+ chars with upper/lower/digit/symbol, PLUS a
   HaveIBeenPwned k-anonymity lookup to reject known-leaked passwords.
4. **Email: format-only validation** (org users). Anonymous users have **no email** — they keep a
   username pattern rule instead.
5. **Fold in the related defects** that undermine auth correctness (see Defects section).
6. **Keep `localStorage` token storage**; httpOnly-cookie migration is a separate, fully-scoped
   appendix task — NOT part of this plan.

## Alternatives Considered

| Alternative | Why rejected |
|---|---|
| Length-only password (NIST style, 12+ chars, no complexity) | You chose complexity + breach check; documented here as the picked policy. |
| Email verification link (prove ownership) | You chose **format-only** and there's no email sender today. Verification + reset are explicitly **out of scope**. |
| Move tokens to httpOnly cookies now | Gateway uses `Access-Control-Allow-Origin "*"` (44×, **0** `Allow-Credentials`), which is incompatible with credentialed cookies. Full rewrite of CORS + `AuthContext` + every service. **Deferred** (Appendix A). |
| Frontend-only validation | Trivially bypassed with curl. Backend MUST re-validate — that's the golden rule below. |

## Scope

**In:**
- Org **signup + login** (`Login.jsx`, `OrganizationService`).
- Anonymous **signup + login** (`AnonymousSignup.jsx`, `AnonymousLogin.jsx`, `AnonymousUserService`).
- Shared email-format + password-strength validation, frontend AND backend.
- HIBP breach check (backend hard gate; frontend soft warning).
- Fold-in defect fixes (Defects section).

**Deferred (explicitly NOT in this plan):**
- Email verification / confirmation links.
- Password reset / change flow (the `/forgot-password` link in `Login.jsx:233` stays a dead link
  for now — note it, don't wire it).
- httpOnly-cookie token storage / CORS rewrite (Appendix A).
- Mid-session 401 auto-refresh / axios interceptor (`AuthContext` only refreshes at app init).

---

## THE GOLDEN RULE (put at the top of both handoffs)

**Frontend validation is UX only. The backend re-validates every rule independently.**
The SPA can be bypassed with curl/devtools, so every rule lives in **two** places: the frontend
for instant feedback, the backend as the real gate. Never trust a client-sent role/id/flag —
derive them server-side from the DB + JWT.

---

## Shared Contract (both Claudes implement to this — identical strings)

### Password policy (applies to BOTH org and anonymous users)
- **Length:** minimum **8**, maximum **64** characters. Do not trim internal spaces; allow them.
- **Complexity:** must contain at least one of EACH — lowercase, uppercase, digit, special char.
  - Canonical regex (use the same on both sides): `^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,64}$`
- **Breach check (HaveIBeenPwned, k-anonymity):**
  - SHA-1 the password, take the first 5 hex chars, GET `https://api.pwnedpasswords.com/range/{prefix5}`,
    look for the remaining 35 chars in the response. **The full password/hash is never sent.**
  - **Backend = hard reject** on signup if found (this is the real gate).
  - **Frontend = soft warning + blocks submit** (UX); if HIBP is unreachable, frontend fails OPEN
    (don't block on network error) and lets the backend decide.

### Email format (org users only)
- Single canonical rule both sides: HTML5 `type="email"` + regex
  `^[^\s@]+@[^\s@]+\.[^\s@]+$`. Lowercase + trim before validating, storing, and comparing.
- Anonymous users have **no email** — keep the existing username rule:
  `^[a-zA-Z0-9_]+$`, length 3–30 (already present at `AnonymousSignup.jsx:96-98`; enforce it in
  `handleSubmit` too, and on the backend).

### Canonical user-facing messages (use verbatim, both sides)
| Code | Message |
|---|---|
| pwd too short | `Password must be at least 8 characters.` |
| pwd too long | `Password must be at most 64 characters.` |
| pwd complexity | `Password must include uppercase, lowercase, a number, and a special character.` |
| pwd breached | `This password has appeared in a data breach. Please choose another.` |
| email format | `Please enter a valid email address.` |
| username format | `Username may only contain letters, digits, and underscores (3–30 chars).` |
| passwords mismatch | `Passwords do not match.` |

### Error-shape reminder (already a codebase rule)
Backend controllers return `BadRequest(new { error = ex.Message })`; the global handler uses
`{ message }`. Frontend's `extractErrorMessage` (in `userService.js:8-18`) already reads
`errors`/`error`/`title` — reuse it; don't introduce a new shape.

---

## Backend section (for backend-Claude)

Follow `.claude/rules/` (controllers-and-errors, repositories-and-dtos, auth-jwt). Work in
**OrganizationService** (org users) and **AnonymousUserService** (anonymous users) — the two
services that own user creation.

1. **Add a shared password-policy validator in the repo layer** of each service (a small private
   helper or a tiny shared static class **copied** into both — there is no shared project, per the
   `AnonymousDomain` duplication note). Called from `CreateUser` (and any future password setter).
   On violation throw `ArgumentException("<canonical message>")` — the controller already maps that
   to `400 { error }`.
2. **Email format + normalization** in `OrganizationService` `UserRepository.CreateUser`: lowercase
   + trim, validate against the canonical regex before the duplicate check. Bad format →
   `ArgumentException`; duplicate email → `InvalidOperationException` → `409 { error }`.
3. **Username format** in `AnonymousUserService` `AnonymousUserRepository.CreateUser`: enforce
   `^[a-zA-Z0-9_]+$`, 3–30. (Duplicate-username check already exists — keep it.)
4. **HIBP breach check (hard gate):**
   - Add a small client following the `Clients/` rule (`IHttpClientFactory` named client, async
     `SendAsync`, linked `CancellationTokenSource`, `System.Text.Json`). Base address
     `https://api.pwnedpasswords.com/`. **No bearer forwarding** (external service).
   - In `CreateUser`, after complexity passes, call it; if the suffix is found → throw
     `ArgumentException` with the canonical "breached" message.
   - **Fail-open on network error** (catch → treat as not-breached) so a HIBP outage can't block
     all signups. Register the named client in each `Program.cs` per the bootstrap rule.
   - **Dependency/risk:** the service container must have outbound internet egress to
     `api.pwnedpasswords.com`. Confirm Docker allows it; if a firewall blocks it, the fail-open
     path keeps signups working (complexity still enforced).
5. **Keep BCrypt hashing** exactly as-is (`AnonymousUserRepository.cs:62`,
   `UserRepository.cs:45`). Validation happens **before** hashing.
6. **Audit logging unchanged** — `CreateUser` paths already log via the controller `TryLogAsync`
   pattern on success/failure. New validation failures flow through the existing `catch` → logged
   with `IsSuccess=false`. No new log verbs needed.
7. **Rate limiting:** the register endpoints in both services already carry
   `[EnableRateLimiting("register")]` (3/hour) and login `("login")` (5/min) — no change.
8. **No new endpoints, no migrations, no gateway changes** — this is validation inside existing
   create flows only.

**Backend self-check (must pass via curl, SPA bypassed):**
- `POST /api/User/` with weak/breached password or malformed email → `400 { error: "<canonical>" }`.
- `POST /api/AnonymousUser/` with weak/breached password or bad username → `400 { error }`.
- Strong, non-breached password + valid email → `201` as today.
- HIBP unreachable → signup with a strong, complex password still succeeds.

---

## Frontend section (for frontend-Claude)

Follow `.claude/rules/` (services-api, forms-validation, data-fetching-state, auth-context,
styling). `API_BASE_URL = 'http://127.0.0.1:80'` (**http** — never the `https://` defect).

1. **New shared validator module** — `src/utils/validation.js`. Export:
   - `validateEmail(email) -> { valid, message }`
   - `validatePassword(password) -> { valid, message }` (length + complexity using the canonical regex)
   - `validateUsername(username) -> { valid, message }`
   - All return the **canonical messages** from the Shared Contract. This is the single source of
     truth — both signup forms import it; delete the duplicated ad-hoc checks.
2. **HIBP soft-check helper** — small function (in `validation.js` or `src/services/pwnedService.js`)
   that does the k-anonymity range request to `https://api.pwnedpasswords.com/range/{prefix}`.
   **Does NOT use `API_BASE_URL`** (external host, not the gateway). Returns "breached / not /
   unknown". On network error → "unknown" (fail open; let backend decide).
3. **Org signup (`Login.jsx` `handleSubmit`, `:107-161`):** add guard clauses after
   `e.preventDefault()` / `setError('')`:
   `validateEmail` → `validatePassword` → (await HIBP soft-check; warn+block if breached) →
   existing role/org checks → service call. Keep the existing inline `{error && <AlertCircle/>}`
   block (`:174-184`). Make the helper text (`:221-223`) match the real enforced rule.
   **Fix:** move `setIsSubmitting(true)` to AFTER the guard clauses return (currently set at `:110`
   before validation, so early `return`s leave the button stuck disabled — real bug).
4. **Anonymous signup (`AnonymousSignup.jsx`):** replace the cosmetic strength logic with hard
   gates from `validatePassword` + `validateUsername` + confirm-match + HIBP soft-check. Keep the
   strength meter for UX but make submission actually enforce the policy.
5. **Anonymous login (`AnonymousLogin.jsx`) defect fix:** replace both `alert()` calls (`:20,44`)
   with the inline error-block pattern used everywhere else (add `const [error,setError]` +
   `{error && <div><AlertCircle/>…}`), per `forms-validation.md`.
6. **`systemUserService.js:1` defect fix:** change `https://127.0.0.1:80` → `http://127.0.0.1:80`
   (the gateway is plain HTTP; it's currently broken).
7. **Login forms** (org + anon, the *login* path, not signup): only minimal "all fields required"
   guards — do NOT run the password-strength validator on login (existing users may predate the
   policy; the strength gate belongs on signup/change only).
8. **No routing/AuthContext changes.** Token stays in `localStorage`. Don't touch the guard tree.

**Frontend self-check:**
- Can't submit org or anon signup with malformed email / weak password (instant inline message,
  no network call). Breached password shows the breach warning and blocks.
- Anonymous login surfaces errors inline (no `alert()`).
- Submit button never gets stuck disabled after a validation early-return.

---

## Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Rules drift between frontend and backend | Shared Contract message table is authoritative; both validators copy it verbatim. |
| HIBP outage blocks all signups | **Fail open** on both sides (network error → allow); complexity is still enforced. |
| HIBP egress blocked in Docker | Fail-open keeps signups working; note egress as a deploy dependency. |
| Existing users have weak passwords (pre-policy) | Only gate **signup** (and future change-password). Never run the strength gate on login. |
| Password policy duplicated in 2 backend services (no shared project) | Accept the copy (matches `AnonymousDomain` duplication reality); keep the two copies byte-identical. |
| Scope creep back into cookies/verification/reset | Those are in Deferred / Appendix A — both Claudes told explicitly to stay out. |

## Verification Plan

- **Backend unit tests** (xUnit, existing test projects): password validator (short / long / missing
  each class / breached-mock / valid), email format, username format. Mock the HIBP client to test
  both "found" and "network-error→allow" branches.
- **Backend integration** (`WebApplicationFactory`): `POST /api/User/` and `POST /api/AnonymousUser/`
  reject weak/breached/bad-format with `400 { error }`; accept valid.
- **Frontend manual:** weak/invalid inputs blocked inline with the canonical message and no network
  call; valid inputs reach the backend; anonymous login shows inline errors; stuck-button bug gone.
- **Bypass test:** curl a weak password straight at the gateway → still `400` (proves backend is the
  real gate, not just the SPA).

## Files Likely to Change

**Backend**
- `OrganizationService/Data/UserRepository.cs` (password + email validation in `CreateUser`)
- `AnonymousUserService/Data/AnonymousUserRepository.cs` (password + username validation in `CreateUser`)
- `OrganizationService/Clients/` + `AnonymousUserService/Clients/` (new HIBP client, copy in both)
- `OrganizationService/Program.cs` + `AnonymousUserService/Program.cs` (register the HIBP named client)
- Corresponding test projects (`OrganizationService*Test*`, `AnonymousUserService*Test*`)

**Frontend**
- `src/utils/validation.js` (new — shared validators)
- `src/services/pwnedService.js` (new, optional — HIBP helper)
- `src/pages/Login.jsx` (enforce validation; fix `setIsSubmitting` ordering; honest helper text)
- `src/pages/AnonymousSignup.jsx` (enforce policy via shared validator)
- `src/pages/AnonymousLogin.jsx` (replace `alert()` with inline error block)
- `src/services/systemUserService.js` (`https://` → `http://`)

## Build / handoff order
1. Both agree on the Shared Contract (regex + messages) — copy it verbatim.
2. Backend ships validation + HIBP gate; verify with curl + unit tests.
3. Frontend builds the shared validator + form gates against the live endpoints.
4. End-to-end: weak/invalid blocked client-side AND server-side; valid passes.

---

## Appendix A — Deferred: httpOnly-cookie token migration (separate task, NOT this plan)

Captured so it isn't lost. Full blast radius:
- Rewrite **all ~21 gateway CORS blocks**: replace `Access-Control-Allow-Origin "*"` with a
  specific origin (`http://localhost:3000`) and add `Access-Control-Allow-Credentials: true`
  (wildcard origin is **incompatible** with credentialed cookies).
- Backend issues the access/refresh token as `Set-Cookie` (HttpOnly, SameSite, Secure-in-prod)
  instead of (or in addition to) the JSON body.
- Rework `AuthContext.js`: it can no longer `jwtDecode` the token from JS (`:28-35`), check expiry
  (`:11-18`), or read it for refresh (`:73-103`). Needs a `/api/User/me`-style endpoint for
  id/email/role, and server-driven refresh.
- Every `fetch` that needs auth switches to `credentials: 'include'`; remove `getAuthHeader()` in
  `userService.js`/`anonymousUserService.js` and the inline token reads in `PublicPortal.jsx`.
- Re-test all 21 services + the guard tree.
**Tradeoff accepted for now:** localStorage tokens are readable by injected JS (XSS theft risk).
Documented and deferred — acceptable for a school-project timeline.

## Appendix B — Related defects NOT folded in (track separately)
- No mid-session 401 auto-refresh (refresh only runs at app init) — would need an interceptor.
- `TrackReport.jsx` mocked, `AnonymousSubmit.jsx` reads a never-written key — pre-existing dead code.
