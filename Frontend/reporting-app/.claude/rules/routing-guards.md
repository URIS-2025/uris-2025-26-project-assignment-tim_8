# Rule: routing & route guards

All routes are declared in **one file**, `src/App.js`, inside a single `<Routes>`. Layouts are applied with `<Outlet>`; access is gated with the in-file `ProtectedRoute` wrapper. Programmatic navigation uses `useNavigate` (14 files, 28 calls).

## The guard (`App.js:28-40`)

```jsx
const ProtectedRoute = ({ children, allowedRoles }) => {
    const { user } = useAuth();
    if (!user) return <Navigate to="/login" replace />;
    if (allowedRoles && !allowedRoles.includes(user.role))
        return <Navigate to="/admin/dashboard" replace />;
    return children;
};
```

## Route tree shape (`App.js:47-90`)

```jsx
<Routes>
  {/* PUBLIC: layout = Navbar + main > Outlet */}
  <Route element={<><Navbar /><main className="main-content"><Outlet /></main></>}>
    <Route path="/" element={<Home />} />
    <Route path="/login" element={<Login />} />
    <Route path="/anonymous/submit" element={<AnonymousSubmit />} />
    <Route path="/organizations" element={<Navigate to="/portal" replace />} />
  </Route>

  {/* ADMIN: guarded, layout = AdminLayout (Sidebar + Outlet) */}
  <Route path="/admin" element={<ProtectedRoute><AdminLayout /></ProtectedRoute>}>
    <Route index element={<Navigate to="/admin/dashboard" replace />} />
    <Route path="boxes/:boxId" element={<BoxDetails />} />
    <Route path="users" element={
        <ProtectedRoute allowedRoles={['admin']}><UserManagement /></ProtectedRoute>} />
  </Route>
</Routes>
```
`AdminLayout.jsx:20` renders `<Outlet />`. `/admin/users` is **double-wrapped** with `allowedRoles={['admin']}` (`App.js:87`).

## WRONG / CORRECT

| WRONG | CORRECT |
|---|---|
| Adding a new `<BrowserRouter>` / nested `<Routes>` in a page | Add a `<Route>` to the existing tree in `App.js` |
| Protecting a page with an ad-hoc `if(!user)` inside the component | Wrap it: `<Route path="..." element={<ProtectedRoute>...</ProtectedRoute>} />` (under `/admin`, it's already guarded by the parent) |
| Role-gating with custom logic | Nest `<ProtectedRoute allowedRoles={['admin']}>` like `/admin/users` |
| `window.location.href = '/x'` or `history.push` | `const navigate = useNavigate(); navigate('/x')` |
| Redirect route via component logic | `<Route path="..." element={<Navigate to="/y" replace />} />` (`App.js:65,74,76`) |
| Comparing roles case-sensitively (`user.role === 'Admin'`) | Roles are lowercase strings (`'admin'`,`'manager'`); see `rules/auth-context.md` |

## Conventions
- **Path params**: `:orgId`, `:boxId`, `:submissionId` — read with `useParams()` (`BoxDetails.jsx:37`).
- **Redirects** always use `<Navigate ... replace />` (not a 200 render) — e.g. `/admin/organizations` → hardcoded `ORG-001` (`App.js:76`).
- **Programmatic nav** examples: `BoxDetails.jsx:253,270,296`, `AdminDashboard.jsx:182,227`, `Sidebar.jsx:77` (`logout(); navigate('/login')`), `AnonymousLogin.jsx:41`.
- New admin page: import it in `App.js`, add `<Route path="..." element={<Page/>} />` under the `/admin` parent. It inherits the guard + `AdminLayout` automatically.
- Sidebar nav visibility is role-filtered separately in `Sidebar.jsx:24-33` (`roles: ['admin']` etc.) — adding a route does **not** add a sidebar link; add it there too.
