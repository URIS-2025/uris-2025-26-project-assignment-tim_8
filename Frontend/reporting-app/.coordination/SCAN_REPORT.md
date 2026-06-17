# SCAN_REPORT.md — Inventory of `Frontend/reporting-app` React SPA

**Task:** 001 (research) · **Base:** `dev` · **Scope:** `Frontend/reporting-app/` only.
All paths repo-relative to the frontend app root. `node_modules/`, `build/`, lockfiles skipped.
Every count below was re-grepped against the working tree (not taken from PLAN.md memory).

## 1. Headline confirmed counts

| Metric | Grep | Count | Notes |
|--------|------|-------|-------|
| Service modules total | `src/services/*.js` | 21 | one plain object per backend resource |
| Services with `API_BASE_URL = 'http://127.0.0.1:80'` | `API_BASE_URL = 'http://127.0.0.1:80'` in `src/services` | 20 | the 21st is `systemUserService.js` (see inconsistency #1) |
| Services with `getAuthHeader` | `getAuthHeader` in `src/services` | **2** | only `userService.js`, `anonymousUserService.js` (inconsistency #2) |
| Pages using `useEffect` | `useEffect` in `src/pages` | 13 | of 21 page files |
| Files importing `lucide-react` | `from 'lucide-react'` in `src` | 24 | sole icon library |
| Files using `useNavigate` | `useNavigate` in `src` | 14 | imperative routing |
| Pages with `setLoading`/`setError` | `setLoading\|setError` in `src/pages` | 8 | local loading/error state |
| `if (!response.ok) throw new Error(...)` in services | escaped grep | 109 across 21 | universal fetch error guard |
| Inline `style={{` occurrences | `style=\{\{` in `src` | 303 across 22 files | (inconsistency #3) |
| `glass-panel` references | `glass-panel` in `src` | 69 across 25 | shared utility class |
| `process.env` usage | `process\.env` in `src` | 0 | base URL is hardcoded, no `.env` |

## 2. Top-level directory structure

| Path | Owns |
|------|------|
| `src/App.js` | All routing, `ProtectedRoute` wrapper, layout composition, `AuthProvider`+`Router` root |
| `src/context/AuthContext.js` | Single auth context: login/logout, `jwt-decode`, localStorage persistence, silent refresh, role resolution |
| `src/services/*.js` (21) | One module per backend resource; plain exported object of async CRUD methods over `fetch` |
| `src/pages/*.jsx` (21) | Full page/route components (Admin dashboard, Public portal, Anonymous flows, Billing, Settings) |
| `src/pages/*.css` (10) | Per-view stylesheets (not every page has one) |
| `src/components/` | Shared UI: `DataTable.jsx`, `Modal.jsx`, `StatusBadge.jsx`, `Navbar.jsx` |
| `src/components/Layout/` | `AdminLayout.jsx` + `Sidebar.jsx` (+ their `.css`) — authenticated shell |
| `src/components/Community/` | `CommunityFeed.jsx` |
| `src/index.css`, `src/App.css` | Global tokens / utility classes (`glass-panel`, `btn`, `icon-btn`, `animate-fade-in`, CSS vars `--accent-primary` etc.) |

## 3. Architecture summary (confirmed)

- **Component architecture:** page-based. Each route renders one `src/pages/*.jsx`; pages compose a small set of shared components (`DataTable`, `Modal`, `StatusBadge`). Confirmed — no atomic-design / feature-folder split.
- **State management:** local `useState` + single `AuthContext`. No Redux/Zustand/Context-besides-Auth/React Query. `useAuth()` consumed in 9 files (`src/App.js:29`, `src/components/Layout/Sidebar.jsx:17`, `src/pages/AdminDashboard.jsx:24`, `src/pages/Login.jsx:50`, `src/pages/AnonymousLogin.jsx:10`, +4).
- **Data fetching:** service modules expose async methods; pages call them inside `useEffect` and store result/loading/error in `useState`. Errors caught, logged via `console.error`, surfaced as `setError(...)` strings with a Retry button (e.g. `src/pages/ProblemBoxes.jsx:45-57,202-208`).
- **Routing:** all declared in `src/App.js`. Public routes share a `<Navbar/> + <Outlet/>` element; `/admin/*` is wrapped in `<ProtectedRoute>` → `<AdminLayout>` (which renders `<Sidebar/>` + `<Outlet/>`). Nested `<ProtectedRoute allowedRoles={['admin']}>` on `/admin/users` (`src/App.js:87`).
- **Styling:** hybrid — per-view `.css` (10 files under `src/pages/`, plus component CSS) + global utility classes + heavy inline `style={{}}`.
- **Forms:** controlled inputs (`value` + `onChange={(e) => setX(...)}`), manual `if (!field) return` validation, `disabled` on submit while pending. No form library.
- **Auth:** `jwt-decode` of `accessToken`, persisted to `localStorage` (`authUser` + `authToken`), silent refresh on expiry, role-based guards.

## 4. Service module shape (the canonical pattern)

Reference: `src/services/problemService.js` (1–59).

```
const API_BASE_URL = 'http://127.0.0.1:80';
export const XService = {
  getAll:  async () => { const r = await fetch(`${API_BASE_URL}/api/X/`); if (!r.ok) throw new Error('...'); return r.json(); },
  getById, create (POST, JSON body, Content-Type), update (PUT), delete (DELETE -> return true)
};
```

| Trait | Evidence |
|-------|----------|
| Hardcoded base URL | `src/services/problemService.js:1`, `src/services/organizationService.js:1`, `src/services/voteService.js:1` (20 total) |
| Raw `fetch`, no axios | `src/services/problemService.js:6,13,27`; `src/services/problemBoxService.js`; `src/services/suggestionService.js` |
| `if (!response.ok) throw new Error(...)` guard | `src/services/problemService.js:7,14,21`; 109 occurrences across all 21 service files |
| Method set `getAll/getById/create/update/delete` | `src/services/problemService.js:5,12,26,39,52`; `src/services/userRoleService.js`; `src/services/organizationService.js` |
| URL comment above each method (`// GET /api/...`) | `src/services/problemService.js:4,11,25`; `src/services/systemUserService.js:4,11,18` |

## 5. Recurring load-bearing patterns (≥3 file:line each)

### P1 — Hardcoded gateway base URL, no env config
- `src/services/problemService.js:1`
- `src/services/organizationService.js:1`
- `src/context/AuthContext.js:5`
- (20 services + AuthContext; `process.env` grep = 0 matches)

### P2 — Fetch + `if (!response.ok) throw new Error(...)` then `return response.json()`
- `src/services/problemService.js:6-8`
- `src/services/problemBoxService.js` (7 occurrences)
- `src/services/userService.js` (9 occurrences)
- (109 occurrences across 21 service files)

### P3 — Page data-fetch lifecycle: `useEffect(() => { fetchX() }, [])` with try/catch/finally + loading/error state
- `src/pages/ProblemBoxes.jsx:40-57`
- `src/pages/Login.jsx:68-87`
- `src/pages/SuggestionBoxes.jsx`, `src/pages/OrganizationDetails.jsx` (setLoading/setError in 8 pages)

### P4 — Error surfaced as glass-panel block + Retry button
- `src/pages/ProblemBoxes.jsx:202-208`
- `src/pages/SuggestionBoxes.jsx`
- `src/pages/BoxDetails.jsx`

### P5 — Controlled input pattern `value={x}` + `onChange={(e) => setX(e.target.value)}`
- `src/pages/ProblemBoxes.jsx:249-251,257-258`
- `src/pages/Login.jsx` (handlers)
- (67 `onChange={(e) => set...` occurrences across 15 files)

### P6 — Manual validation guard then proceed
- `src/pages/ProblemBoxes.jsx:69` (`if (!formData.name || !formData.organizationId) return;`)
- `src/pages/Login.jsx` (submit handler)
- `src/pages/SuggestionBoxes.jsx`

### P7 — Native `window.confirm` / `alert` for destructive ops & error UX
- `src/pages/ProblemBoxes.jsx:93,86,99`
- `src/pages/UserManagement.jsx` (4)
- `src/pages/OrganizationDetails.jsx` (9)
- (36 occurrences across 9 pages)

### P8 — `DataTable` driven by a `columns` config array (header/accessor/render)
- `src/pages/ProblemBoxes.jsx:103-159` (defines `columns`)
- `src/components/DataTable.jsx:80-100` (consumes `columns`, `col.render`)
- Also used by `SuggestionBoxes.jsx`, `UserManagement.jsx`, `OrganizationDetails.jsx`, `BoxDetails.jsx`, `BillingDashboard.jsx` (6 pages)

### P9 — `Modal` (isOpen/onClose/title/children/footer), Esc-to-close, glass-panel overlay
- `src/components/Modal.jsx:5,7-19`
- `src/pages/ProblemBoxes.jsx:226-242`
- Used by ~7 pages (SuggestionBoxes, UserManagement, OrganizationDetails, BoxDetails, BoxSettings, SubscriptionDetails)

### P10 — Shared utility classes (`glass-panel`, `btn`/`btn-ghost`/`btn-primary`, `icon-btn`, `animate-fade-in`)
- `src/components/DataTable.jsx:48,64`
- `src/components/Modal.jsx:24,26,31`
- `src/components/Layout/AdminLayout.jsx:11`, `src/components/Layout/Sidebar.jsx:36`
- (`glass-panel` 69 occurrences across 25 files)

### P11 — lucide-react icons (named imports, `size=` prop)
- `src/components/DataTable.jsx:2`
- `src/components/Layout/Sidebar.jsx:3-12`
- `src/pages/ProblemBoxes.jsx:6`
- (24 importing files)

### P12 — Imperative nav via `useNavigate` on row-click / actions / logout
- `src/pages/ProblemBoxes.jsx:20,162`
- `src/components/Layout/Sidebar.jsx:18,77`
- (14 files use `useNavigate`)

## 6. Routing table (`src/App.js`)

| Path | Element | Guard |
|------|---------|-------|
| `/` | `Home` | public (Navbar+Outlet) |
| `/login`, `/signup` | `Login` | public |
| `/portal` | `PublicPortal` | public |
| `/track` | `TrackReport` | public |
| `/anonymous/signup` | `AnonymousSignup` | public |
| `/anonymous/login` | `AnonymousLogin` | public |
| `/anonymous/submit` | `AnonymousSubmit` | public |
| `/organizations` | redirect → `/portal` | public |
| `/admin` | `AdminLayout` | `ProtectedRoute` (any logged-in) |
| `/admin/dashboard` | `AdminDashboard` | protected |
| `/admin/organizations/:orgId` | `OrganizationDetails` | protected |
| `/admin/boxes/:boxId` | `BoxDetails` | protected |
| `/admin/boxes/:boxId/settings` | `BoxSettings` | protected |
| `/admin/submissions/:submissionId` | `SubmissionDetails` | protected |
| `/admin/billing`, `/admin/billing/:orgId` | `BillingDashboard` / `SubscriptionDetails` | protected |
| `/admin/community` | `CommunityDashboard` | protected |
| `/admin/suggestions` | `SuggestionBoxes` | protected |
| `/admin/problems` | `ProblemBoxes` | protected |
| `/admin/users` | `UserManagement` | `ProtectedRoute allowedRoles={['admin']}` (nested) |
| `/admin/settings` | `Settings` | protected |

`ProtectedRoute` (`src/App.js:28-40`): no `user` → `Navigate to="/login"`; role not in `allowedRoles` → `Navigate to="/admin/dashboard"`. Sidebar nav additionally filters items by `item.roles.includes(role)` (`src/components/Layout/Sidebar.jsx:24-33`).

## 7. Auth pattern (`src/context/AuthContext.js`)

| Step | Evidence |
|------|----------|
| Decode JWT | `jwtDecode(accessToken)` line 28; XML-soap claim URIs lines 30-35 |
| Resolve role via API | `UserRoleService.getById(roleId)` line 40 → lowercased `title` |
| Persist | `localStorage.setItem('authUser', ...)` + `'authToken'` lines 58-59 |
| Logout | `removeItem('authUser'/'authToken')` lines 69-70 |
| Token expiry check | `isTokenExpired` lines 11-18 (`exp*1000 < Date.now()`) |
| Silent refresh | lines 73-103, endpoint `/api/AnonymousUser/refresh` vs `/api/User/refresh` chosen by `!roleId` |
| Init on mount | `useEffect(... initializeAuth, [])` lines 105-138 |
| Context value | only `{ user, login, logout }` exposed (line 141) |
| Auth header read | `localStorage.getItem('authToken')` in `userService.js:4`, `anonymousUserService.js:4`, and inline in `src/pages/PublicPortal.jsx:168,524,535` |

## 8. Inconsistencies / risks to flag

| # | Issue | Evidence |
|---|-------|----------|
| 1 | **`systemUserService.js` uses `https://127.0.0.1:80`** (all 20 others use `http://`) — likely a typo that breaks calls through the http gateway | `src/services/systemUserService.js:1` |
| 2 | **Only 2 of 21 services attach auth headers** (`getAuthHeader`). The other 19 call protected-ish endpoints unauthenticated | `getAuthHeader` only in `src/services/userService.js`, `src/services/anonymousUserService.js`; pages also read token ad-hoc (`PublicPortal.jsx:168,524,535`) |
| 3 | **Mixed styling: per-view `.css` vs heavy inline styles** — 303 inline `style={{}}` across 22 files, with only 10 page CSS files; `StatusBadge.jsx` is 100% inline | `src/components/StatusBadge.jsx:49-63`; `style={{` 303 occurrences |
| 4 | **Hardcoded base URL, no `process.env`** — environment cannot be switched without editing 21 files + AuthContext | `process.env` = 0 matches; 20× `API_BASE_URL` literal |
| 5 | **Two error-handling tiers in services** — most just `throw new Error('static message')`; only `userService.js` has `extractErrorMessage` parsing `data.errors`/`data.title` | `src/services/userService.js:8-18` vs `src/services/problemService.js:7` |
| 6 | **Native `alert`/`window.confirm` for UX** (36 occurrences) — no toast/modal-confirm abstraction | `src/pages/ProblemBoxes.jsx:86,93,99` |
| 7 | **Role naming mismatch** — Sidebar references `'billingmanager'` role for Billing nav, but `ProtectedRoute` default redirect assumes `/admin/dashboard` access for all roles | `src/components/Layout/Sidebar.jsx:30` vs `src/App.js:36` |

## 9. Proposed rule files (per PLAN.md — 7)

| File | Scope (one line) |
|------|------------------|
| `.claude/rules/services-api.md` | Service-module contract: `export const XService`, hardcoded `API_BASE_URL`, raw fetch, `if (!response.ok) throw`, method naming |
| `.claude/rules/data-fetching-state.md` | Page fetch lifecycle: `useEffect([])` + try/catch/finally, `loading`/`error` `useState`, error-panel + Retry |
| `.claude/rules/routing-guards.md` | Centralized `src/App.js` routes, `ProtectedRoute` + `allowedRoles`, `Outlet` layouts, `useNavigate` for imperative nav |
| `.claude/rules/auth-context.md` | `AuthContext` usage: `useAuth()`, localStorage `authUser`/`authToken`, jwt-decode, silent refresh, role resolution |
| `.claude/rules/shared-components.md` | `DataTable` columns-config, `Modal` props/Esc-close, `StatusBadge` types — reuse before inventing UI |
| `.claude/rules/styling.md` | Utility classes (`glass-panel`/`btn`/`icon-btn`/`animate-fade-in`) + CSS vars first; when inline styles are acceptable |
| `.claude/rules/forms-validation.md` | Controlled inputs, manual `if (!x) return` validation, `disabled` on submit, `form-control`/`form-group` markup |

## 10. Proposed guide files (per PLAN.md — 5)

| File | Scope (one line) |
|------|------------------|
| `.claude/guides/architecture-overview.md` | Dir map, page-based architecture, state/data/routing/styling stacks, file index |
| `.claude/guides/auth-flow.md` | End-to-end login → token decode → persist → guard → silent refresh → logout |
| `.claude/guides/api-contract-mapping.md` | Each `src/services/*.js` ↔ backend gateway `/api/*` endpoints, with the systemUser https quirk |
| `.claude/guides/admin-dashboard.md` | `/admin/*` shell (`AdminLayout`/`Sidebar`), DataTable/Modal-driven CRUD pages, role-gated nav |
| `.claude/guides/anonymous-flow.md` | `/anonymous/*` signup/login/submit + `/track`, `anonymousUserService`, anonymous token path |

## 11. Stack facts (re-confirmed)

React 19, react-router-dom 7 (CRA / react-scripts 5), `jwt-decode` 4, `lucide-react` (24 importers).
NO Redux/Zustand/React Query/Tailwind/styled-components/form library/axios. State = local `useState` + single `AuthContext`.
