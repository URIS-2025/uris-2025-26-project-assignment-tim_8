# Results: Auth input hardening — valid emails + strong passwords (org + anonymous)

**Coordination branch:** coordinate/auth-input-hardening
**Base branch:** feature/auth-input-hardening
**Compiled:** 2026-06-01
**Mode:** local

---

## Narrative summary

The goal was to make auth input validity guaranteed at the server (the only layer that matters)
and consistent in the SPA, for both organization users (email + password) and anonymous users
(username + password). Previously the React forms *advertised* strong-input rules but never
enforced them, and the backend `CreateUser` repos hashed whatever they received — so anything
bypassing the SPA (curl/devtools) was accepted.

Ten tasks ran as parallel agents in isolated git worktrees. The backend now enforces a single
password policy (8–64 chars, upper/lower/digit/symbol) plus a HaveIBeenPwned k-anonymity breach
check, and per-service identity rules (email format for org, username pattern for anon) — all in
the repository layer, before BCrypt, throwing `ArgumentException` mapped to `400 { error }`. The
frontend got a single source-of-truth validator module (`validation.js` + `pwnedService.js`) wired
into both signup forms as instant UX gates that mirror the backend messages verbatim, while login
paths keep only a fields-present guard (existing users may predate the policy). Three pre-existing
defects were folded in: `alert()` → inline errors in anonymous login, a broken `https://` gateway
URL, and a stuck submit-button bug.

The built-in review surfaced — and a follow-up task fixed — a contract-breaking defect that no
individual implementation task could see on its own: the create DTOs carried DataAnnotations that,
under `[ApiController]`, auto-returned `400 { errors: {...} }` (wrong shape, non-canonical messages,
and even a *different* password regex) *before* the canonical repo gate ran. Task 010 removed those
overlapping annotations and made the repository the single source of truth, so every invalid input
now returns the canonical `400 { error }`.

Net effect: the backend is now the real gate (proven by integration tests that POST straight at the
endpoints), the frontend is a faithful UX mirror, and the rules live in two places that are
verified byte-identical — including the U+2013 en-dash in the username message.

---

## Key findings

- **The server is now the real gate.** `POST /api/User/` and `POST /api/AnonymousUser/` reject
  malformed/weak/breached/empty input with `400 { error: "<canonical>" }` and accept valid input
  with `201` — verified through the HTTP pipeline, not just unit-mocked.
- **A dual-validation-layer bug was hiding behind green unit tests.** DTO DataAnnotations +
  `[ApiController]` intercepted most invalid inputs with the wrong shape/message before the repo
  ran. It was invisible to tasks 001/002 (their tests bypassed the HTTP model-binding pipeline),
  caught only by integration task 003, fully diagnosed by review 008, and fixed by task 010.
- **Fail-open breach checking.** Both `PwnedPasswordsClient` copies swallow any network/HTTP error
  and return "not breached", so an HIBP outage (or blocked Docker egress) can never block signups —
  complexity is still enforced.
- **Message parity verified across three surfaces** (org backend, anon backend, frontend),
  byte-for-byte including the en-dash — the anti-drift seam the plan demanded actually holds.
- **~20 backend tests are red, but all pre-existing on base** (see Remaining work) — none are
  regressions from this work.

---

## Changes per task

| # | Task | Type | Branch | Review | Summary |
|---|---|---|---|---|---|
| 001 | backend-org-validation | code | task/001-backend-org-validation | (via 008) | Canonical `PasswordPolicy` + `PwnedPasswordsClient`; email+complexity+breach in `UserRepository.CreateUser`; unit tests. |
| 002 | backend-anon-validation | code | task/002-backend-anon-validation | (via 008) | Byte-identical copies into AnonymousUserService; username+complexity+breach; **added missing `catch(ArgumentException)→400`**; reconciled draft test. |
| 003 | backend-integration-tests | code | task/003-backend-integration-tests | (via 008) | `WebApplicationFactory` tests for both endpoints; surfaced the dual-layer defect. |
| 004 | frontend-shared-validator | code | task/004-frontend-shared-validator | PASS (009) | `validation.js` + `pwnedService.js`; single source of truth, fail-open HIBP. |
| 005 | frontend-org-signup-gates | code | task/005-frontend-org-signup-gates | PASS (009) | `Login.jsx` gates; stuck-button fix; honest helper text. |
| 006 | frontend-anon-signup-gates | code | task/006-frontend-anon-signup-gates | PASS (009) | `AnonymousSignup.jsx` real gates; kept meter as UX only. |
| 007 | frontend-defect-fixes | code | task/007-frontend-defect-fixes | PASS (009) | `AnonymousLogin.jsx` alert()→inline; `systemUserService.js` https→http. |
| 008 | review-backend | research | — | **CONCERNS** | Diagnosed the dual-validation-layer contract break; recommended repo-as-single-source. |
| 009 | review-frontend | research | — | PASS | All checks pass; flagged the org email-message nit (fixed by 010). |
| 010 | fix-validation-layer | code | task/010-fix-validation-layer | — | Removed overlapping DTO annotations; `SuppressModelStateInvalidFilter`; null-guards; strict tests. Supersedes 001/002/003. |

---

## Cross-cutting observations

These only became visible by reading all ten task outputs together:

- **`task/010` subsumes the entire backend.** Ancestry checks confirm 001, 002, and 003 are all
  ancestors of 010 (003 already merged 001+002; 010 branched off 003). **Merge only `task/010` for
  the backend** — merging 001/002/003 separately is redundant and would muddy history. Likewise
  `task/004` is an ancestor of both 005 and 006, so the frontend collapses to 005+006+007. The
  8 code branches reduce to **4 branches to merge** with **zero file overlap between them**.

- **Classic parallel-agent failure mode, caught by design.** Tasks 001 and 002 each finished with
  green unit tests, yet the end-to-end contract was broken — their tests mocked the validator and
  never exercised the `[ApiController]` model-binding pipeline. A single sequential developer might
  also have missed it, but here it was the *integration* task (003) and the dedicated *review* task
  (008) — both downstream of the implementers — that exposed and diagnosed it. The review layer
  earned its place: without 008/010 the feature would have shipped returning `{ errors }` with a
  weaker, lowercase-less password regex for most real rejections.

- **The same ~20 red tests were reported independently by every backend agent** (001, 002, 003,
  010), which is strong evidence they are a *systemic pre-existing* condition, not a regression:
  ~14 OrganizationService + ~5 AnonymousUserService `WebApplicationFactory` tests return `401`
  because non-register endpoints are `[Authorize]` and the test host sends no JWT, plus 1 stale
  `DeleteAnonymousUser_DoesNotThrow_WhenNotFound` (the repo now throws `KeyNotFoundException`). This
  work merely *revealed* a latent gap in the integration-test harness.

- **Test-heavy, code-light — scope stayed tight.** The backend diff (task/010 vs base) is
  +1094/-62, but production code is only ~280 lines (two ~70-line HIBP clients, two 25-line policy
  validators, small repo/controller/Program edits) — the other ~800 lines are tests. The `-62`
  removals are the deleted DTO annotations and a few test-password updates. No scope creep; the
  combined frontend change is ~137 net new lines across 6 files.

- **Operational dependency that spans both services:** breach checking requires outbound egress to
  `api.pwnedpasswords.com` from both OrganizationService and AnonymousUserService containers. Fail-
  open means a blocked egress degrades *silently* (no breach rejection, complexity still enforced) —
  worth a deploy-time note so the degradation isn't mistaken for "HIBP working."

---

## Remaining work

- **Pre-existing integration-test 401s (not introduced here):** give the `WebApplicationFactory`
  hosts a valid JWT (or a Testing-env auth bypass) so the ~19 `[Authorize]` integration tests on
  non-register endpoints go green. Recommend a dedicated follow-up task.
- **Stale unit test:** `AnonymousUserRepositoryTests.DeleteAnonymousUser_DoesNotThrow_WhenNotFound`
  contradicts the repo's current throw-on-missing behavior — update or remove it.
- **Deferred by the plan (explicitly out of scope, not done):** email verification/confirmation,
  password reset/change flow (the `/forgot-password` link stays dead), httpOnly-cookie token
  migration + CORS rewrite (Appendix A), and mid-session 401 auto-refresh (Appendix B).
- **Manual E2E:** front-to-back submit/login, a real HIBP "breached" rejection, and backend-message
  surfacing need a running stack — not exercisable in the worktrees.

---

## Recommended merge order

Into `feature/auth-input-hardening`. All four branches touch disjoint files → no conflicts expected.

| Order | Branch | Files touched | Conflict risk |
|---|---|---|---|
| 1 | task/010-fix-validation-layer | OrganizationService + AnonymousUserService (validation, clients, repos, controller, DTOs, Program.cs) + both test projects | none (backend only) |
| 2 | task/004-frontend-shared-validator | `src/utils/validation.js`, `src/services/pwnedService.js` | none |
| 3 | task/005-frontend-org-signup-gates | `src/pages/Login.jsx` (+ carries 004's files) | none — shares 004 as ancestor |
| 4 | task/006-frontend-anon-signup-gates | `src/pages/AnonymousSignup.jsx` (+ carries 004's files) | none — disjoint page from 005 |
| 5 | task/007-frontend-defect-fixes | `src/pages/AnonymousLogin.jsx`, `src/services/systemUserService.js` | none |

**Do NOT merge** task/001, task/002, task/003 — they are ancestors of task/010 and would be
redundant. Step 2 (task/004) is optional since 005/006 already contain it, but merging it first
keeps the frontend history readable.

---

## Coverage audit

All planned scope items are covered exactly once, no omissions or duplicates:

| Scope item (from PLAN.md) | Covered by |
|---|---|
| Org signup validation (email + password + breach) | 001 → 010 |
| Org login (fields-only guard) | 005 |
| Anon signup validation (username + password + breach) | 002 → 010 |
| Anon login (inline errors, fields-only guard) | 007 |
| Shared FE validator + HIBP soft-check | 004 |
| FE org signup gates + stuck-button fix | 005 |
| FE anon signup gates | 006 |
| Defect: systemUserService https→http | 007 |
| Backend integration / bypass test | 003 → 010 |
| Single canonical `{ error }` contract | 010 (post-review fix) |

All 10 task files are in `completed` state. No `unclaimed`/`in-progress`/`blocked` remain.
