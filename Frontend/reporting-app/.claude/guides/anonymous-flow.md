# Guide: anonymous & public flow

The public-facing side: browse organizations/boxes, submit a report anonymously, and track it. All routes are unauthenticated and use the `Navbar` + `Outlet` public layout (`App.js:49-66`).

## Routes (public)
| Path | Page | Purpose |
|---|---|---|
| `/portal` | `PublicPortal` | browse organizations / boxes / suggestions, vote |
| `/track` | `TrackReport` | look up a submission by code |
| `/anonymous/signup` | `AnonymousSignup` | create an anonymous account |
| `/anonymous/login` | `AnonymousLogin` | log in anonymously |
| `/anonymous/submit` | `AnonymousSubmit` | submit a problem/suggestion |

## Intended journey
1. **Signup** (`AnonymousSignup.jsx`): `AnonymousUserService.create({username,password})` → navigate to `/anonymous/login`. Client validation: username present, password ≥ 8, passwords match (`:32-45`).
2. **Login** (`AnonymousLogin.jsx`): `AnonymousUserService.login()` → `{accessToken,refreshToken}` → `AuthContext.login()` (writes `authUser` + `authToken`; anonymous because the JWT carries no `roleId`).
3. **Submit** (`AnonymousSubmit.jsx`): pick a box, post a Problem/Suggestion tied to that box.
4. **Track** (`TrackReport.jsx`): enter a tracking code to see status.

## Box access links
`boxAccessLinkService.js` (`/api/BoxAccessLink`, owned by AnonymousUserService) provides the link/token mechanism that ties an anonymous submission to a specific box. It is read-oriented in the SPA.

## Key entities
- Anonymous identity = a JWT with **no `roleId`** (governs which refresh endpoint is used — see `guides/auth-flow.md`).
- Submissions reference a `boxId` (suggestion box or problem box); `status`/`priority` are numeric enums (`statusMap`/`priorityMap`).

## Gotchas / known bugs (verify before relying on these flows)
- **`AnonymousSubmit.jsx:33` reads `localStorage.getItem('anonymousUser')`** — a key **nothing in the codebase ever writes** (`AuthContext.login` writes `authUser`/`authToken`, not `anonymousUser`). So the gate at `:34` always redirects to `/anonymous/login`; the standalone `/anonymous/submit` page is effectively dead. The **working** anonymous submission path is **`PublicPortal.jsx`**, which decodes `authToken` directly (`:168,524,535`) and posts there.
- **`TrackReport.jsx` is fully mocked** — it uses `setTimeout` + hardcoded data, no service call. It does not hit the backend.
- These are real inconsistencies; treat the `PublicPortal` path as the source of truth for anonymous submission and fix `AnonymousSubmit`/`TrackReport` rather than copying them.

## Extension points
- New public page: add a `<Route>` inside the public layout group in `App.js:49-66`.
- New anonymous-facing API: add a service module per `rules/services-api.md`; most public endpoints are unauthenticated (no `getAuthHeader`).

## Files read
`src/pages/AnonymousSignup.jsx`, `src/pages/AnonymousLogin.jsx`, `src/pages/AnonymousSubmit.jsx`, `src/pages/TrackReport.jsx`, `src/pages/PublicPortal.jsx` (referenced), `src/services/anonymousUserService.js`, `src/services/boxAccessLinkService.js`, `src/services/problemService.js`, `src/services/suggestionService.js`, `src/context/AuthContext.js`, `src/App.js`.
