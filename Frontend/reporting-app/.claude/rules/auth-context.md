# Rule: auth context & token handling

Auth lives in **one** place: `src/context/AuthContext.js`. Read it with `useAuth()`; never re-implement token decode/storage in components.

## What `useAuth()` gives you (`AuthContext.js:141`)

```js
const { user, login, logout } = useAuth();   // exactly these three
```
`user` is `null` when logged out, otherwise: `{ id, email, name, roleId, role, token, refreshToken }` (`AuthContext.js:47-55`). 9 consumers: `App.js:29`, `Login.jsx:50`, `AnonymousLogin.jsx:10`, `AdminDashboard.jsx:24`, `Sidebar.jsx:17`, `OrganizationDetails.jsx:23`, `CommunityDashboard.jsx:12`, `Settings.jsx:7`, `SubmissionDetails.jsx:26`.

## WRONG / CORRECT

| WRONG | CORRECT |
|---|---|
| `JSON.parse(localStorage.getItem('authUser'))` in a component | `const { user } = useAuth();` |
| After login: `localStorage.setItem('authUser', ...)` by hand | `const { login } = useAuth(); await login(loginResponse);` — it decodes + persists |
| `jwtDecode(token)` in a component to read the role | `user.role` (already decoded + resolved) |
| `if (user.role === 'Admin')` | `if (user.role === 'admin')` — role is **lowercased** (`AuthContext.js:41`) |
| Building your own logout (clearing storage manually) | `const { logout } = useAuth(); logout();` (`AuthContext.js:67-71`) |
| Checking `user.isAnonymous` (doesn't exist) | Anonymous == **no `roleId`** (`AuthContext.js:76`) |

## How `login()` works (`AuthContext.js:23-65`) — don't reimplement, just call it
- Accepts `{accessToken, refreshToken}` **or** a bare token string: `loginResponse?.accessToken ?? loginResponse`.
- Decodes with `jwtDecode`. Claims are read by their full XML-schema URIs:
  - id → `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`
  - email → `.../claims/emailaddress` (falls back to `.../claims/name`)
  - role id → `decoded['RoleId'] || decoded['role'] || '.../claims/role'`
- Resolves the human role via `UserRoleService.getById(roleId)` → `roleData.title.toLowerCase()`, default `'user'` (`AuthContext.js:37-45`).
- Persists to localStorage: key **`authUser`** (the full JSON object) and key **`authToken`** (raw access token).

## Token storage keys (the only two)
| Key | Value | Read by |
|---|---|---|
| `authUser` | JSON of the user object | `AuthContext` init (`:107`) |
| `authToken` | raw JWT access token | services `userService.js:4`, `anonymousUserService.js:4`; (ad-hoc) `PublicPortal.jsx:168,524,535` |

Services attach it via `getAuthHeader()` reading `localStorage.getItem('authToken')` — see `rules/services-api.md`.

## Session restore & refresh (`AuthContext.js:73-138`)
- On mount, reads `authUser`; if `isTokenExpired` (`exp*1000 < Date.now()`), calls `silentRefresh`.
- `silentRefresh` POSTs to **`/api/User/refresh`** when `roleId` is present (org user) or **`/api/AnonymousUser/refresh`** when absent (anonymous) (`AuthContext.js:76-79`). Failure → `logout()`.
- Gotcha: there is no axios interceptor — refresh only runs at app init, not on a mid-session 401.

See `guides/auth-flow.md` for the end-to-end sequence.
