# Guide: frontend architecture overview

## What this is
`Frontend/reporting-app` is the React 19 SPA for the Anonymous Reporting System. It is a Create React App (`react-scripts` 5) project that talks to 11 ASP.NET Core backend services **only** through the Nginx gateway.

## Entry points
- `src/index.js` → renders `<App />` in `<React.StrictMode>` into `#root` (`public/index.html`).
- `src/App.js` → `AuthProvider` > `BrowserRouter` > `<Routes>`. All routing is here. See `rules/routing-guards.md`.

## Stack (from `package.json`)
| Concern | Choice |
|---|---|
| Framework | React 19.2 |
| Build/dev | react-scripts 5 (CRA) — `start`/`build`/`test`/`eject` |
| Routing | react-router-dom 7.13 |
| Auth decode | jwt-decode 4 |
| Icons | lucide-react |
| State | local `useState` + one `AuthContext` — **no** Redux/Zustand/React Query |
| Styling | plain CSS + tokens in `src/index.css` — **no** Tailwind/CSS-modules |
| Forms | native `<form>` — **no** form library |

## Layering & data flow
```
pages/*.jsx  ──calls──▶  services/*.js  ──fetch──▶  http://127.0.0.1:80/api/*  (Nginx gateway)
   │                                                          │
   uses useState/useEffect for loading/error          routes to one of 11 backend services
   │
 components/ (DataTable, Modal, StatusBadge, Navbar, Sidebar)
 context/AuthContext  ── JWT in localStorage, role-based guards
```
- A page fetches in `useEffect`, stores result in `useState`, renders via shared components. See `rules/data-fetching-state.md`.
- Cross-cutting auth state is the single exception to "no global store": `context/AuthContext.js`.

## Commands
```bash
cd Frontend/reporting-app
npm install
npm start      # dev server on http://localhost:3000
npm test       # react-scripts test (Jest, watch mode)
npm run build  # production build to build/
```

## Critical gotcha: the gateway dependency
The dev server runs on **:3000**, but every service module hardcodes the API base as `http://127.0.0.1:80` (the Nginx gateway). Backend services are **not** individually exposed — only the gateway (`:80`) and SQL Server (`:1433`). So any API-touching work requires the Docker stack running:
```bash
docker-compose up -d   # from repo root, alongside `npm start`
```
There is **no** `.env` and **zero** `process.env` usages — the base URL is hardcoded in 21 service files + `AuthContext.js:5`. To change environments you edit those constants. (`systemUserService.js:1` wrongly uses `https://` — see `guides/api-contract-mapping.md`.)

## File map (`src/`)
| Path | Owns |
|---|---|
| `index.js`, `App.js`, `index.css`, `App.css` | bootstrap, routing, global styles/tokens |
| `pages/` (~27 files) | full screens: Home, Login, PublicPortal, TrackReport, Anonymous{Signup,Login,Submit}, AdminDashboard, OrganizationDetails, BoxDetails, BoxSettings, SubmissionDetails, BillingDashboard, SubscriptionDetails, ProblemBoxes, SuggestionBoxes, UserManagement, CommunityDashboard, Settings |
| `components/` | `DataTable`, `Modal`, `StatusBadge`, `Navbar`, `Layout/{AdminLayout,Sidebar}`, `Community/CommunityFeed` |
| `services/` (21 files) | one module per backend resource (see `guides/api-contract-mapping.md`) |
| `context/AuthContext.js` | auth state, JWT, localStorage, role resolution |

## Files read to write this guide
`package.json`, `src/index.js`, `src/App.js`, `src/index.css`, `src/services/problemService.js`, `src/services/anonymousUserService.js`, `src/context/AuthContext.js`, `src/components/DataTable.jsx`.
