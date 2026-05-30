# CLAUDE.md — reporting-app (frontend)

React 19 SPA for the Anonymous Reporting System. It renders the public portal, anonymous submission/tracking flows, and the protected `/admin` back-office, talking to 11 ASP.NET Core backend services **only** through the Nginx gateway at `http://127.0.0.1:80/api/*`. Create React App (`react-scripts` 5); no Redux/Tailwind/form-library — local `useState` + one `AuthContext`, plain CSS, native forms.

> Backend guidance lives in the repo-root `../../CLAUDE.md`. This file covers `Frontend/reporting-app/` only.

## Commands
```bash
cd Frontend/reporting-app
npm install
npm start      # dev server on http://localhost:3000
npm test       # react-scripts test (Jest + Testing Library, watch mode)
npm run build  # production build → build/
```
```bash
# API-touching work needs the gateway + services running (from repo root):
docker-compose up -d
```
No lint script and no `.env` — there is no separate lint step (CRA eslint runs in `start`/`build`), and the API base URL is hardcoded in every service module + `AuthContext`.

## Architecture
| Layer | Technology | Purpose |
|---|---|---|
| Bootstrap | `src/index.js` → `src/App.js` | StrictMode render; `AuthProvider` > `BrowserRouter` > `<Routes>` |
| Routing | react-router-dom 7 | all routes in `App.js`; `ProtectedRoute` + `Outlet` layouts |
| Pages | `src/pages/*.jsx` | full screens; own their data via `useState`/`useEffect` |
| Components | `src/components/` | `DataTable`, `Modal`, `StatusBadge`, `Navbar`, `Layout/` |
| Services | `src/services/*.js` (21) | one plain-object module per backend resource; raw `fetch` |
| State | local `useState` + `context/AuthContext.js` | no global store except auth |
| Auth | jwt-decode + localStorage | JWT decode, role resolution, guards |
| Styling | plain CSS + tokens in `src/index.css` | utility classes (`glass-panel`, `btn`, `icon-btn`) |
| Icons | lucide-react | only icon library |
| Gateway | Nginx `:80` | routes `/api/*` to backend services (not individually exposed) |

## Rules — how to write code here (`.claude/rules/`)
| File | Scope |
|---|---|
| `services-api.md` | Service-module shape, `API_BASE_URL`, fetch + `throw on !ok`, URL conventions, the auth-header gap, the `https://` defect |
| `data-fetching-state.md` | `useState` loading/error + `useEffect` fetch; early-return loading/error UI; keep server data local |
| `routing-guards.md` | Routes in `App.js`, `ProtectedRoute`, `allowedRoles`, `Navigate replace`, `useNavigate`, sidebar sync |
| `auth-context.md` | `useAuth()` `{user,login,logout}`, localStorage keys, lowercase roles, refresh endpoints |
| `shared-components.md` | `DataTable` columns config, `Modal`, `StatusBadge` prop contracts; lucide-react icons |
| `styling.md` | CSS-per-view + global tokens/utilities; inline `style={{}}` only for runtime-computed values |
| `forms-validation.md` | Native `<form>`, guard-clause validation in `handleSubmit`, inline error block (not `alert()`) |

## Guides — how the system works (`.claude/guides/`)
| File | Topic |
|---|---|
| `architecture-overview.md` | Stack, layering, data flow, commands, gateway dependency, `src/` file map |
| `auth-flow.md` | login → JWT decode → role resolve → localStorage → silent refresh → guards; org vs anonymous |
| `api-contract-mapping.md` | 21 services ↔ `/api/*` routes ↔ 11 backend services; DTO/enum shapes; error tiers |
| `admin-dashboard.md` | `/admin` layout, sub-routes, DataTable/Modal data flow, role-based sidebar |
| `anonymous-flow.md` | Public portal + anonymous signup/login/submit/track; the dead `AnonymousSubmit` path |

The original inventory scan is in `.coordination/SCAN_REPORT.md`.

## Constraints (hard rules specific to this codebase)
- **API base is hardcoded `http://127.0.0.1:80`** in all 21 `src/services/*.js` + `AuthContext.js:5`. No `.env`, zero `process.env`. `systemUserService.js:1` wrongly uses `https://` — it's broken; don't copy it.
- **Backend services are not individually exposed** — only the gateway (`:80`) and SQL Server (`:1433`). API work requires `docker-compose up -d`.
- **All routing in `src/App.js`** — never add a second `<BrowserRouter>`/`<Routes>`. Guard pages with `ProtectedRoute`; role-gate by nesting `allowedRoles`.
- **Auth only via `useAuth()`** — never parse `authUser`/`authToken` or call `jwtDecode` in a component. Roles are **lowercase** strings.
- **No global store, no form library, no CSS framework** — local `useState`, native `<form>`, plain CSS with tokens from `src/index.css`.
- **Service calls must `throw` on `!response.ok`** so pages' `try/catch` can set error state.
- **Only `userService.js` + `anonymousUserService.js` send auth headers** — add `getAuthHeader()` deliberately for protected endpoints.
- Known dead/mocked code: `AnonymousSubmit.jsx` (reads a never-written `anonymousUser` key), `TrackReport.jsx` (mocked) — use `PublicPortal` as the working anonymous path.
