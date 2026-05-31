# Plan: Auth input hardening — valid emails + strong passwords (org + anonymous)

Base branch: feature/auth-input-hardening
Mode: local
Created: 2026-05-31T19:24:00+02:00

Source plan: `AUTH_HARDENING_PLAN.md` (read it for full rationale). This file is the static
breakdown; progress lives in the `tasks/` filenames, never here.

## THE GOLDEN RULE (applies to every task)

Frontend validation is UX only. **The backend re-validates every rule independently.** The SPA
can be bypassed with curl/devtools, so every rule lives in two places: frontend for instant
feedback, backend as the real gate. Never trust a client-sent role/id/flag.

## Shared Contract (every task copies these VERBATIM — identical strings both sides)

**Password policy (org AND anonymous):**
- Length: min 8, max 64. Do not trim internal spaces; allow them.
- Complexity regex (same on both sides):
  `^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,64}$`
- Breach check: SHA-1 the password, take first 5 hex chars (uppercase), GET
  `https://api.pwnedpasswords.com/range/{prefix5}`, search response lines for the remaining
  35 hex chars. Full password/hash never sent. Backend = HARD reject on signup. Frontend = soft
  warning + block submit. **Fail OPEN on network error** (both sides) — never block on outage.

**Email format (org users only):** lowercase + trim, then regex `^[^\s@]+@[^\s@]+\.[^\s@]+$`.
Anonymous users have NO email — username rule instead: `^[a-zA-Z0-9_]+$`, length 3–30.

**Canonical user-facing messages (verbatim, both sides):**
| Code | Message |
|---|---|
| pwd too short | `Password must be at least 8 characters.` |
| pwd too long | `Password must be at most 64 characters.` |
| pwd complexity | `Password must include uppercase, lowercase, a number, and a special character.` |
| pwd breached | `This password has appeared in a data breach. Please choose another.` |
| email format | `Please enter a valid email address.` |
| username format | `Username may only contain letters, digits, and underscores (3–30 chars).` |
| passwords mismatch | `Passwords do not match.` |

**Error shape:** backend controllers return `BadRequest(new { error = ex.Message })`. Frontend's
`extractErrorMessage` (`src/services/userService.js:8-18`) reads `errors`/`error`/`title` — reuse it.

## Important repo facts (verified — the plan is slightly wrong on these)

- **`.claude/rules/` does NOT exist** in this repo. The source plan tells you to "follow
  `.claude/rules/...`" — those files are absent. Conventions are inlined in each task instead.
- **`OrganizationService/Controllers/UserController.cs` Create** has a generic
  `catch (Exception) -> BadRequest(new { error })` (lines ~67-80), so a repo-thrown
  `ArgumentException` already maps to 400. ✅
- **`AnonymousUserService/Controllers/AnonymousUserController.cs` Create** (lines ~44-57) ONLY
  catches `InvalidOperationException -> 409`. There is NO `ArgumentException` catch — a validation
  throw would bubble to the global handler. Task 002 MUST add the 400 catch. ⚠️
- A draft test `AnonymousUserServiceTest/AnonymousUserCreationDtoValidationTests.cs` was committed
  on the base branch. It uses **DataAnnotations** validation, which CONFLICTS with the plan's
  repo-layer-validator approach and currently fails (the DTO has no annotations). Task 002 must
  reconcile it (rewrite to test the repo-layer validator, or delete in favor of new tests).

## Tasks

| # | Name | Type | Dependencies |
|---|---|---|---|
| 001 | backend-org-validation | code | — |
| 002 | backend-anon-validation | code | 001 |
| 003 | backend-integration-tests | code | 001, 002 |
| 004 | frontend-shared-validator | code | — |
| 005 | frontend-org-signup-gates | code | 004 |
| 006 | frontend-anon-signup-gates | code | 004 |
| 007 | frontend-defect-fixes | code | — |
| 008 | review-backend | research | 001, 002, 003 |
| 009 | review-frontend | research | 004, 005, 006, 007 |

Immediately claimable at start: 001, 004, 007.

## Acceptance criteria (overall)

- `POST /api/User/` and `POST /api/AnonymousUser/` reject weak/breached passwords and bad
  email/username with `400 { error: "<canonical message>" }`; accept strong+valid as today (201).
- HIBP unreachable -> signup with a strong, complex password still succeeds (fail-open).
- Frontend org + anon signup block malformed email / weak password inline (canonical message, no
  network call); breached password shows warning and blocks submit.
- `AnonymousLogin.jsx` surfaces errors inline (no `alert()`); submit button never stuck disabled
  after a validation early-return; `systemUserService.js` uses `http://` not `https://`.
- BCrypt hashing unchanged; no new endpoints, no migrations, no gateway changes; tokens stay in
  localStorage (cookie migration is deferred — Appendix A, out of scope).
- Login paths (org + anon) get only "all fields required" guards — NEVER run the strength gate on
  login (existing users may predate the policy).
