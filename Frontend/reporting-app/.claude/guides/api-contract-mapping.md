# Guide: API contract mapping (frontend ↔ backend)

There are **no shared TypeScript types**. The contract between the React app and the backend is implicit in (a) each service module's `fetch` URL and (b) the JSON shape pages consume. This guide is the map.

## How a call reaches a backend service
```
ResourceService.method()  →  fetch http://127.0.0.1:80/api/<Resource>/…  →  Nginx gateway (:80)  →  <backend service>:8080
```
All 21 modules hardcode `const API_BASE_URL = 'http://127.0.0.1:80'` (gateway). The gateway routes `/api/<Controller>/` to the owning service by container name.

## Service module → route → backend service
| `src/services/…` | Route prefix | Backend service (owner) |
|---|---|---|
| `anonymousUserService.js` | `/api/AnonymousUser` | AnonymousUserService |
| `boxAccessLinkService.js` | `/api/BoxAccessLink` | AnonymousUserService |
| `organizationService.js` | `/api/Organization` | OrganizationService |
| `userService.js` | `/api/User` | OrganizationService |
| `userRoleService.js` | `/api/UserRole` | OrganizationService |
| `systemUserService.js` | `/api/SystemUser` | OrganizationService |
| `problemService.js` | `/api/Problem` | ProblemService |
| `problemCategoryService.js` | `/api/ProblemCategory` | ProblemService |
| `problemCommentService.js` | `/api/ProblemComment` | ProblemService |
| `problemBoxService.js` | `/api/ProblemBox` | ProblemBoxService |
| `suggestionService.js` | `/api/Suggestion` | SuggestionService |
| `suggestionCategoryService.js` | `/api/SuggestionCategory` | SuggestionService |
| `suggestionCommentService.js` | `/api/SuggestionComment` | SuggestionService |
| `voteService.js` | `/api/Vote` | SuggestionService |
| `suggestionBoxService.js` | `/api/SuggestionBox` | SuggestionBoxService |
| `subscriptionService.js` | `/api/Subscription` | SubscriptionService |
| `subscriptionPlanService.js` | `/api/SubscriptionPlan` | SubscriptionService |
| `paymentService.js` | `/api/Payment` | SubscriptionService |
| `billingNotificationService.js` | `/api/BillingNotification` | BillingNotificationService |
| `systemNotificationService.js` | `/api/SystemNotification` | SystemNotificationService |
| `attachmentService.js` | `/api/Attachment` | AttachmentService |

(LoggerService is backend-only; the SPA never calls it. AttachmentService also exposes `/api/Attachment/suggestion/{id}` and `/api/Attachment/problem/{id}`.)

## DTO / response shapes the frontend depends on
| Endpoint | Frontend expects |
|---|---|
| `POST /api/User/login`, `/api/AnonymousUser/login`, `…/refresh` | `{ accessToken, refreshToken }` |
| `GET /api/UserRole/{id}` | object with `.title` (→ lowercased role) |
| JWT claims | XML-schema URIs for id/email + `RoleId` (see `guides/auth-flow.md`) |
| Problem/Suggestion `status` | numeric `0..4` → `statusMap` {New, In Progress, Reviewing, Resolved, Closed} (`BoxDetails.jsx:13-19`) |
| Problem/Suggestion `priority` | numeric `0..3` → `priorityMap` {Low, Medium, High, Critical} (`BoxDetails.jsx:22-27`) |
| `GET /api/Suggestion` then filter | client filters by `s.suggestionBoxId === boxId` (`BoxDetails.jsx:82`) |

## Error contract (two tiers)
- **Rich** (only `userService.js`, `anonymousUserService.js`): `extractErrorMessage` parses `{ errors }` (field map) ‖ `{ error }` ‖ `{ title }` ‖ `'Request failed'`.
- **Plain** (other 19): `throw new Error('Failed to <verb> <resource>')` — a fixed string.
Pages surface `err.message` in their error UI (`rules/data-fetching-state.md`).

## Auth on requests
Only `userService.js` and `anonymousUserService.js` send `Authorization: Bearer <authToken>` via `getAuthHeader()`. The other 19 send no credentials. See `rules/services-api.md`.

## Gotchas / defects
- **`systemUserService.js:1` uses `https://127.0.0.1:80`** — every other module uses `http://`. The gateway is plain HTTP on :80, so SystemUser calls fail. Real bug.
- No generated client, no OpenAPI types — a backend route rename silently breaks the matching string here. Grep `/api/<Controller>` to find the consumer.
- Numeric enum maps are duplicated per page (`BoxDetails.jsx`, `AnonymousSubmit.jsx:10`) rather than centralized.

## Files read
All 21 files in `src/services/`, plus `src/context/AuthContext.js`, `src/pages/BoxDetails.jsx`, `src/pages/AnonymousSubmit.jsx`. (Repo-root `gateway/nginx.conf` and backend `CLAUDE.md` are outside this app dir; backend ownership taken from the project service catalog.)
