# Organization Notifications

Two streams reach the org dashboard: **system** notifications (submissions, comments,
membership/role changes, votes) and **billing** notifications (plan changes, payments,
cancellations). The display side (`OrganizationDetails.jsx`) fetches both via `getAll()` and
filters client-side by `organizationId`. **Production happens in the backend.**

## Producers

| Event | Producer | Org resolution |
|-------|----------|----------------|
| New problem / problem comment | ProblemService (ProblemController, ProblemCommentController) | problem.ProblemBoxId → GET /api/ProblemBox/{id} → OrganizationId |
| New suggestion / comment / vote | SuggestionService (Suggestion/SuggestionComment/Vote controllers) | suggestion.SuggestionBoxId → GET /api/SuggestionBox/{id} → OrganizationId |
| Member added / role change | OrganizationService (UserController) | OrganizationId read directly off the DTO (no lookup); role change only fires when RoleId actually changed |
| Plan change (fan-out 1/org), payment success/fail, cancellation(=delete) | SubscriptionService | subscription.OrganizationId |

## Rules of the road

- Notification + box-lookup calls are non-fatal — see `.claude/rules/service-to-service-calls.md`.
- Box lookup must NOT throw: if the box GET fails/returns null, skip the notification silently.
- Notification controllers are `[Authorize]`d; producers forward the caller's bearer.
- Both notification services validate JWTs from **multiple issuers** (AnonymousUserService AND
  OrganizationService) so anon-user and org-user forwarded tokens both validate. Keys/issuers
  live in each notification service's `appsettings.json` `Jwt` section.
- Comment notifications are produced **once, by the backend**. The frontend must NOT also create
  one (the old `SubmissionDetails.jsx` create was removed in this work).

## Create DTOs (contract)

| Notification | Route | Body fields the receiver binds |
|---|---|---|
| System | POST /api/SystemNotification | Text, OrganizationId?, AnonymousUserId?, ProblemCommentId?, SuggestionCommentId? |
| Billing | POST /api/BillingNotification | Text, OrganizationId, PaymentId (required — use Guid.NewGuid() for non-payment events) |

Each producer service keeps its own local copy of the create-DTO under `Clients/` (like
`LogCreationDTO`), not a cross-project reference.
