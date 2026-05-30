# Guide: authentication flow

All auth logic is in `src/context/AuthContext.js` (146 lines). Two kinds of identity share it: **organization users** (have a `roleId`) and **anonymous users** (no `roleId`).

## Entry points
- `AuthProvider` wraps the app in `App.js:44`; exposes `{ user, login, logout }` via `useAuth()`.
- Org login: `pages/Login.jsx` (calls `UserService.login` / org auth, then `login(response)`).
- Anonymous login: `pages/AnonymousLogin.jsx` (calls `AnonymousUserService.login`).
- Guard: `ProtectedRoute` in `App.js:28-40`.

## Sequence (login → guarded page)
1. A page calls a service `login(credentials)` which returns `{ accessToken, refreshToken }` (`anonymousUserService.js:58-66`; org equivalent in `userService.js`).
2. The page calls `await login(loginResponse)` (`AuthContext.js:23`). `login` accepts the object **or** a bare token string (`:24`).
3. `jwtDecode(accessToken)` (`:28`). Claims read by full URIs (`:30-35`):
   - `id` ← `…/claims/nameidentifier`
   - `email`/`name` ← `…/claims/emailaddress` ‖ `…/claims/name`
   - `roleId` ← `decoded['RoleId'] || decoded['role'] || …/claims/role`
4. If `roleId` present, `UserRoleService.getById(roleId)` → `roleData.title.toLowerCase()`; default `'user'` (`:37-45`).
5. Build `user = { id, email, name, roleId, role, token, refreshToken }`; `setUser(user)` and persist:
   - `localStorage['authUser']` = JSON(user) (`:58`)
   - `localStorage['authToken']` = accessToken (`:59`)
6. `ProtectedRoute` reads `user`: `null` → `<Navigate to="/login">`; role not in `allowedRoles` → `<Navigate to="/admin/dashboard">` (`App.js:31-37`).

## Session restore (app init, `AuthContext.js:105-138`)
- Reads `authUser`. If `isTokenExpired(token)` (`exp*1000 < Date.now()`, `:11-18`) → `silentRefresh`.
- Else, if `roleId` exists but `role` missing, re-resolve role via `UserRoleService.getById`, then `setUser`.

## Silent refresh (`AuthContext.js:73-103`)
```js
const isAnonymous = !storedUser.roleId;
const endpoint = isAnonymous ? '/api/AnonymousUser/refresh' : '/api/User/refresh';
// POST { refreshToken } → { accessToken, refreshToken }; on !ok → return null → logout()
```
| Identity | Distinguished by | Refresh endpoint |
|---|---|---|
| Organization user | has `roleId` | `POST /api/User/refresh` |
| Anonymous user | no `roleId` | `POST /api/AnonymousUser/refresh` |

## Key entities
| Thing | Where | Notes |
|---|---|---|
| `user` object | `AuthContext` state + `authUser` key | `role` is a lowercased string |
| access token | `authToken` key | read by services via `getAuthHeader()` |
| roles | `'admin'`, `'manager'`, `'user'`, `'billingmanager'` | from `UserRole.title`, lowercased |

## Gotchas
- **Anonymous == absence of `roleId`** — there is no `isAnonymous` flag.
- **Tokens in localStorage** → XSS-readable (accepted tradeoff).
- **Refresh only at init** — no HTTP interceptor; a mid-session 401 won't auto-refresh.
- Role compares must be lowercase (`user.role === 'admin'`).
- `logout()` clears both `authUser` and `authToken` and nulls state (`:67-71`).
- `Sidebar.jsx:21` reads `user?.role || 'user'` and filters nav by role; gating a route does not gate its sidebar link (add it in `Sidebar.jsx:24-31`).

## Files read
`src/context/AuthContext.js`, `src/App.js`, `src/pages/AnonymousSignup.jsx`, `src/pages/AnonymousSubmit.jsx`, `src/services/anonymousUserService.js`, `src/services/userRoleService.js`, `src/components/Layout/Sidebar.jsx`.
