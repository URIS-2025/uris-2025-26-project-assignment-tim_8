# Results: Organization Notifications (System + Billing)

**Coordination branch:** coordinate/organization-notifications
**Base branch:** feature/auth-input-hardening
**Compiled:** 2026-06-02
**Mode:** local

---

## Narrative summary

Organizations were supposed to receive two notification streams — **system** notifications
(submissions, comments, membership/role changes, votes) and **billing** notifications
(plan changes, payments, cancellations) — but almost nothing produced them. The display side
already worked (`OrganizationDetails.jsx` fetches both streams via `getAll()` and filters by
`organizationId`); the gap was **production**, and in one case a **duplicate** production.

Seven tasks closed that gap. Task 001 wrote a binding **contract** (DTO shapes, routes, the
non-fatal client template, the `[Authorize]`+JWT-forwarding rule, and the box→org resolution
rule) that every other task followed. Tasks 002–005 added backend **producers**: each domain
service now fires a notification through a service-to-service HTTP call when a relevant event
occurs, mirroring the existing `LoggerServiceClient.TryLogAsync` pattern — **non-fatal**
(try/catch-swallow + 800 ms timeout) so a notification failure never breaks the underlying
operation, and **JWT-forwarding** so the call survives once the endpoints require auth. Task
006 locked the two notification controllers behind `[Authorize]` and wired multi-issuer JWT
validation. Task 007 removed the now-duplicate frontend comment-notification create, leaving
the backend as the single producer.

The result: every in-scope event now produces exactly one notification with the correctly
resolved `organizationId`, notifications never break creates, and the notification endpoints
reject unauthenticated callers while authenticated producer/display traffic keeps working.

The six code branches touch **completely disjoint files** (one service each), so they merge
without conflict in any order — but they share a **runtime contract** that dictates how they
must be deployed together (see Cross-cutting observations).

---

## Key findings

- **The contract held.** All three services that needed a system-notification DTO (002, 003,
  004) produced byte-identical 13-line local copies, and all five backend producers use the
  identical non-fatal client shape. Task 001 successfully prevented divergence across five
  independently-worked tasks.
- **Zero file overlap across all six code branches.** Each task is scoped to a single service
  (or the one frontend file). The combined change is conflict-free.
- **One runtime cross-dependency, not a git one:** task 006's `[Authorize]` only works because
  producers 002–005 forward the caller's bearer. Confirmed present in every producer.
- **Data-model reality forced design choices in 005:** `SubscriptionPlan` has no price field,
  payments carry no status, and cancellation == delete — so "plan/price change" = plan PUT,
  payment success/fail = create try/catch, cancellation = delete. Documented in the task file.
- **Task 004's task file is incomplete** (empty `## Solution` / `## Branch`) even though the
  branch `task/004-organizationservice-producer` exists, the code is correct and
  contract-compliant, and its tests pass (**78/78**, verified during this compile).
  Documentation gap only — see Remaining work.
- **Combined size:** ~1,715 insertions / ~37 deletions across 55 files. Roughly half is tests
  (002: +161 test lines, 003: +271, 004: +117, 005: +238, 006: ~+190). Scope did not balloon
  beyond the plan.

---

## Changes per task

| # | Task | Type | Branch | Verification | Summary |
|---|---|---|---|---|---|
| 001 | notification-contract | both | — (contract only) | n/a | Locked DTOs/routes, non-fatal client template, `[Authorize]`+JWT rule, box→org resolution rule. Unblocked all others. |
| 002 | problemservice-producer | code | task/002-problemservice-producer | 22 tests pass | Notify on new problem + new problem comment; org via `ProblemBox`. |
| 003 | suggestionservice-producer | code | task/003-suggestionservice-producer | 49 tests pass | Notify on new suggestion, comment, and vote; org via `SuggestionBox`. |
| 004 | organizationservice-producer | code | task/004-organizationservice-producer ⚠️ | **78 tests pass** (verified in compile) | Notify on member-added + role-change (guarded on real RoleId change); org direct off DTO. Code correct; **task file lacks Solution/Branch**. |
| 005 | subscriptionservice-billing-producers | code | task/005-subscriptionservice-billing-producers | 102 tests pass | Plan-update fan-out (1/ subscribed org), payment success/fail, cancellation; made create call non-fatal; new PUT + by-planId lookup. |
| 006 | notification-controllers-authorize | code | task/006-notification-controllers-authorize | System 12/12, Billing 31/31 | `[Authorize]` on both notification controllers + multi-issuer JWT wiring; tests attach valid bearer. |
| 007 | frontend-comment-dedup | code | task/007-frontend-comment-dedup | `npm run build` clean | Removed duplicate `SystemNotificationService.create` in `SubmissionDetails.jsx` (+ unused import/var). |

*No `## Review` sections were present on any task — no `/task-review` pass was run. The
"Verification" column reflects each task's own test run; 002/003/004/007 were also
re-verified during this compile.*

---

## Cross-cutting observations

1. **Git-clean but deploy-coupled.** The six branches share no files, so any merge order is
   conflict-free. But there is a hard **runtime ordering** for deployment: task 006 makes
   `POST /api/SystemNotification` and `POST /api/BillingNotification` return **401 without a
   valid bearer**. If 006 is deployed before producers 002–005 forward the JWT, every
   notification silently 401s (non-fatal → silently dropped). All producers *do* forward
   `Request.Headers["Authorization"]`, so the safe deploy is "producers and 006 together."
   This dependency is invisible in any single task file — it only appears when you read 006
   against 002–005.

2. **The contract is empirically validated, not just asserted.** Three independent agents
   (002/003/004) each created `Clients/SystemNotificationCreationDTO.cs`; diffing them shows
   they are identical. Five producers each reimplemented the non-fatal client; all use the
   same 800 ms timeout + try/catch-swallow + `virtual`/parameterless-ctor-for-mocking shape.
   The Phase-0 contract did its job — there is nothing to reconcile.

3. **Multi-issuer JWT is the load-bearing detail for end-to-end correctness.** 006 accepts
   tokens from **both** `AnonymousUserService` and `OrganizationService` issuers. This matters
   because the producers forward *whatever* token the caller sent: an anonymous user submitting
   a problem forwards an anon token, an org user forwards an org token, and the org page reads
   notifications with an org token. A single-issuer config would have dropped a whole class of
   notifications. No single producer task could have known this was required.

4. **"Exactly once" is now an end-to-end property, not a per-task one.** Before this work,
   comment notifications were produced by the frontend (002/003 would have made that *two*).
   Task 007 is the sequencing keystone: it depends on 002+003 and removes the frontend create.
   The "exactly one notification per event" acceptance criterion is only satisfied by the
   **combination** of 002+003 (produce) and 007 (stop double-producing) — verifiable only here.

5. **Resolution-path consistency.** 002 and 003 resolve org through a box lookup
   (problem/suggestion → BoxId → box service → org) and skip silently on box-miss; 004 reads
   org directly off the DTO; 005 resolves through subscription → org. Three different
   resolution strategies, all converging on the same contract guarantee (no throw, skip on
   unresolved). Consistent by design across agents who never saw each other's code.

6. **Test posture, reconciled.** 003/005/006 added/repaired substantial suites (006 also fixed
   two *pre-existing* base-branch test failures as a side effect). 004 added 117 test lines but
   never recorded its result in the task file — the parallel workflow's "agent forgot to
   document" failure mode. This compile ran those tests directly: **78/78 pass**, so the gap is
   documentation only, not correctness.

---

## Remaining work

- **Task 004 documentation gap (cosmetic):** the task file has empty `## Solution` / `## Branch`.
  The code on `task/004-organizationservice-producer` is correct, contract-compliant, and its
  tests pass (78/78, run during this compile). Optionally backfill the task file's resolution
  fields for the record; no code action needed.
- **No independent review pass.** No `/task-review` was run; verifications above are test runs,
  not code review. Consider a review of 004/005/006 (the three completed by parallel agents)
  before merge if this is going to production.
- **Deferred from the plan (intentional, not done):** new-box-created notifications; making GET
  endpoints org-scoped from the JWT (still `getAll()` + client filter); read-state UX,
  notification dedup/batching, real-time push. Billing fan-out is N calls (no batching) by
  design for v1.
- **Pre-existing frontend lint debt (out of scope):** `npm run build` under `CI=true` fails on
  pre-existing unused-vars in unrelated files (`SuggestionService`, `payments`, `mockManagers`)
  — not introduced by 007.

---

## Recommended merge order

All six branches are off the same base and touch disjoint files, so **git merge order is
free** (zero conflict risk). The order below is by *runtime safety* — land the producers and
the gateway-auth change as one set, then the dedup:

| Merge order | Branch | Files touched | Conflict risk |
|---|---|---|---|
| 1 | task/002-problemservice-producer | ProblemService/*, ProblemServiceTests/* | none |
| 2 | task/003-suggestionservice-producer | SuggestionService/*, SuggestionServiceTests/* | none |
| 3 | task/004-organizationservice-producer | OrganizationService/*, OrganizationServiceTests/* | none |
| 4 | task/005-subscriptionservice-billing-producers | SubscriptionService/*, SubscriptionServiceTests/* | none |
| 5 | task/006-notification-controllers-authorize | BillingNotificationService/*, SystemNotificationService/* (+tests) | none — **deploy with 1–4, not before** |
| 6 | task/007-frontend-comment-dedup | Frontend/.../SubmissionDetails.jsx | none — depends on 002+003 being live |

Merge-order caveat is about **deployment**, not git: do not ship 006 to an environment whose
producer services predate 1–4, and do not ship 007 to a frontend whose backend predates 002+003.

---

## Coverage audit

Expected producers/events from PLAN.md, cross-referenced against the branches' diffs:

| Event (plan scope) | Owner | Implemented? |
|---|---|---|
| New problem submitted | 002 | ✅ `ProblemController.CreateProblem` |
| New problem comment | 002 | ✅ `ProblemCommentController.CreateProblemComment` |
| New suggestion submitted | 003 | ✅ `SuggestionController.CreateSuggestion` |
| New suggestion comment | 003 | ✅ `SuggestionCommentController.CreateSuggestionComment` |
| Suggestion vote | 003 | ✅ `VoteController.CreateVote` |
| Member added / role change | 004 | ✅ `UserController` Create + Update (role-change guarded) |
| Plan/price change fan-out | 005 | ✅ `SubscriptionPlanController` PUT + by-planId fan-out |
| Payment success / failure | 005 | ✅ `PaymentController.CreatePayment` try/catch |
| Subscription cancellation | 005 | ✅ `SubscriptionController.Delete` |
| Make create billing call non-fatal | 005 | ✅ `BillingServiceCall` rewritten |
| `[Authorize]` both notification controllers | 006 | ✅ both + multi-issuer JWT |
| Remove duplicate frontend comment create | 007 | ✅ `SubmissionDetails.jsx` |

**Missing: none. Duplicates: none.** Every in-scope event has exactly one producer, and the
one duplicate path (frontend comment notification) was removed. All seven task branches build
and their unit suites pass (002: 22, 003: 49, 004: 78, 005: 102, 006: 12+31, 007: build clean).
