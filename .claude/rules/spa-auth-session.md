# SPA Auth Session (AuthContext)

`context/AuthContext.js` owns the JWT: it stores `{authToken, authUser}` in **localStorage**
(persists across reloads and browser restarts) and rehydrates on mount.

## Rehydrate before guarding routes — don't confuse "loading" with "logged out"

`AuthProvider` starts with `user = null` and restores it **asynchronously** in a `useEffect`.
A route guard that only checks `!user` redirects to `/login` on the first render — before
rehydration finishes — so every page reload logs the user out. The token was never the
problem; the missing state was.

| Pattern | Evidence |
|---------|----------|
| `initializing` flag (default true → false in the init effect's `finally`), exposed via context | `context/AuthContext.js` |
| Guard renders a loading state while `initializing`, redirects only after | `App.js` `ProtectedRoute` |

```jsx
// WRONG — redirects during async rehydration, logs the user out on reload
const ProtectedRoute = ({ children }) => {
  const { user } = useAuth();
  if (!user) return <Navigate to="/login" replace />;
  return children;
};

// CORRECT — wait for rehydration to finish first
const ProtectedRoute = ({ children }) => {
  const { user, initializing } = useAuth();
  if (initializing) return <div className="app-loading">Loading…</div>;
  if (!user) return <Navigate to="/login" replace />;
  return children;
};
```

Reaching for `sessionStorage` to "fix logout on reload" is a false fix — it weakens
persistence (cleared when the tab closes) and does not address the race.

## Calls to `[Authorize]` endpoints must send the bearer

Each service file has a local `getAuthHeader()` reading `authToken` from localStorage; spread
it into the `headers` of every mutating fetch. A missing header is a silent 401.

| Pattern | Evidence |
|---------|----------|
| `getAuthHeader()` on create/update/delete | `services/userService.js`, `services/organizationService.js` |
