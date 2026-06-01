# Plan: Organization Notifications (System + Billing)

Base branch: feature/auth-input-hardening
Mode: local
Created: 2026-06-01

## Problem

Organizations should receive two notification streams but almost nothing produces them:
- **System notifications** are created only from the frontend (`SubmissionDetails.jsx:195`) when staff reply to a comment. No backend producer exists for submissions, comments, membership changes, or votes.
- **Billing notifications** are created only on brand-new subscription creation (`SubscriptionController.Create`, line ~48). Plan/price changes, payments, and cancellations produce nothing.

The display side already works (`OrganizationDetails.jsx` fetches both via `getAll()` and filters client-side by `organizationId`). The gap is **production**, not display.

## Chosen approach

Move notification production into the backend. Each domain service fires a notification through a service-to-service HTTP call when a relevant event occurs — mirroring the existing `BillingServiceCall` + named `HttpClient` factory pattern. Notification calls are **non-fatal** (try/catch + swallow, like `LoggerServiceClient.TryLogAsync`) so a notification failure never breaks the underlying operation. Notification endpoints get `[Authorize]`; producers forward the caller's JWT (`Request.Headers["Authorization"]`).

## Tasks

| # | Name | Type | Dependencies |
|---|---|---|---|
| 001 | notification-contract | both | — |
| 002 | problemservice-producer | code | 001 |
| 003 | suggestionservice-producer | code | 001 |
| 004 | organizationservice-producer | code | 001 |
| 005 | subscriptionservice-billing-producers | code | 001 |
| 006 | notification-controllers-authorize | code | 001 |
| 007 | frontend-comment-dedup | code | 002, 003 |

001 (Phase 0 contract) blocks everything. 002–006 run in parallel once 001 is `completed`. 007 (frontend dedup) waits for 002 and 003 so the backend produces comment notifications before the duplicate frontend create is removed.

## Scope

**In:**
- System notifications: new problem submitted, new suggestion submitted, new comment (problem & suggestion, any author), new manager/user added or role change, suggestion vote.
- Billing notifications: plan/price change (fan-out to all subscribed orgs), payment success/fail, subscription cancellation.
- New SubscriptionPlan **update** endpoint + a "subscriptions by planId" lookup to drive fan-out.
- `[Authorize]` on both notification controllers + JWT forwarding in all producers.
- Make existing subscription-create billing call non-fatal.
- Unit tests for every producer and the fan-out logic.
- Frontend cleanup: remove the now-duplicate comment-notification create in `SubmissionDetails.jsx`.

**Deferred:**
- New problem box / suggestion box created (excluded for v1).
- Subscription-created notification (already works — untouched, only made non-fatal).
- Making GET endpoints org-scoped from the JWT (still `getAll()` + client filter).
- Read-state UX, notification dedup/batching, real-time push (still poll-on-load).

## Acceptance criteria

- Each in-scope event produces exactly one notification with the correctly-resolved `organizationId` and a sensible `Text`.
- Org-id is resolved from `BoxId` via the box service (`GET api/<Box>/{id}` → `OrganizationId`); comments/votes resolve through parent → box → org.
- All notification calls are non-fatal: notification client throwing does NOT break the underlying create/update.
- Both notification controllers enforce `[Authorize]` (401 without a token); producers forward the caller's bearer token.
- Plan/price change fans out one billing notification per subscribed org.
- Unit tests pass for every producer, the org-resolution path (incl. box-missing no-throw), the non-fatal path, and the fan-out count.
- `dotnet build AnonymousApp.sln` and `dotnet test AnonymousApp.sln` succeed.
- Duplicate frontend comment-notification create removed; comment notifications still appear on the org page exactly once.
