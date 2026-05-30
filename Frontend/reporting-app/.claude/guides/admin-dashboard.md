# Guide: admin dashboard area (`/admin/*`)

The protected back-office. Everything under `/admin` is gated and shares one layout.

## Entry & layout
- Route group: `App.js:69` — `<Route path="/admin" element={<ProtectedRoute><AdminLayout/></ProtectedRoute>}>`.
- `components/Layout/AdminLayout.jsx` renders `<Sidebar />` + `<main><Outlet/></main>` (`Outlet` at `:20`).
- `components/Layout/Sidebar.jsx` builds nav from a role-filtered `navItems` array (`:24-33`) and reads `user` via `useAuth()`.

## Sub-routes (`App.js:74-88`)
| Path | Page | Notes |
|---|---|---|
| `dashboard` | `AdminDashboard` | landing (index redirects here) |
| `organizations/:orgId` | `OrganizationDetails` | `/admin/organizations` redirects to hardcoded `ORG-001` (`:76`) |
| `boxes/:boxId` | `BoxDetails` | suggestion or problem box + its items |
| `boxes/:boxId/settings` | `BoxSettings` | |
| `submissions/:submissionId` | `SubmissionDetails` | |
| `billing`, `billing/:orgId` | `BillingDashboard`, `SubscriptionDetails` | |
| `community`, `suggestions`, `problems` | `CommunityDashboard`, `SuggestionBoxes`, `ProblemBoxes` | |
| `users` | `UserManagement` | **double-guarded** `allowedRoles={['admin']}` (`:87`) |
| `settings` | `Settings` | |

## Page data-flow pattern
Every admin page follows the same shape (see `rules/data-fetching-state.md`):
```
useEffect → ResourceService.getAll()/getById() → setState(data) / setError / setLoading
   → build a `columns` config → <DataTable data={data} columns={columns} onRowClick=… />
   → row/action opens <Modal> for view/edit/create  (rules/shared-components.md)
```
- `DataTable` search & pagination are controlled by the page's own `useState` (`searchValue`, `currentPage`, `onPageChange`).
- `BoxDetails.jsx` is the reference page: dual-fetch (tries `SuggestionBoxService` then falls back to `ProblemBoxService`, `:63-74`), maps numeric `status`/`priority` to labels, renders rows into `DataTable`, navigates to `/admin/submissions/:id` on row click (`:253`).
- `UserManagement.jsx:99-215` is the reference for DataTable + Modal CRUD together.

## Role model in the sidebar (`Sidebar.jsx:24-31`)
| Nav item | Visible to roles |
|---|---|
| Dashboard, Users | `['admin']` |
| Community Board, Suggestion Boxes, Problem Boxes | `['admin','manager']` |
| Billing | `['billingmanager']` |
| Settings, Sign Out | always (footer) |

## Gotchas
- `/admin/organizations` → hardcoded `ORG-001` redirect; deep-linking the bare path won't pick the user's org.
- Billing nav gates on role `'billingmanager'`, but the Billing **route** itself is only behind the generic `/admin` guard (any logged-in user can hit `/admin/billing` by URL). Route-level role gating exists only on `/admin/users`.
- Adding an admin page = add the `<Route>` in `App.js` **and** a `navItems` entry in `Sidebar.jsx`, else it's reachable but invisible.

## Files read
`src/App.js`, `src/components/Layout/AdminLayout.jsx`, `src/components/Layout/Sidebar.jsx`, `src/components/DataTable.jsx`, `src/components/Modal.jsx`, `src/pages/AdminDashboard.jsx`, `src/pages/BoxDetails.jsx`, `src/pages/OrganizationDetails.jsx`, `src/pages/UserManagement.jsx`.
