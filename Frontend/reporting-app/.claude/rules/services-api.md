# Rule: service / API layer (`src/services/*.js`)

All backend access goes through a service module in `src/services/`. There are **21** of them, one per backend resource. Each is a plain exported object — never a class, never axios, never a hook.

## The module shape (copy it exactly)

```js
const API_BASE_URL = 'http://127.0.0.1:80';          // verbatim, top of every file

export const ProblemService = {                       // PascalCase <Resource>Service
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/Problem/`);
        if (!response.ok) throw new Error('Failed to fetch problems');
        return await response.json();
    },
    // getById / create / update / delete ...
};
```
Evidence: `export const XService = {` in 21 files — `problemService.js:3`, `anonymousUserService.js:20`, `userService.js:20`. `if (!response.ok) throw` appears **109 times across 21 files** (`problemService.js:7,14,21,34,47,56`).

## URL conventions

| Operation | Path form | Example (`problemService.js`) |
|---|---|---|
| GET collection | trailing slash | `` `${API_BASE_URL}/api/Problem/` `` |
| GET by id | append `${id}`, no trailing slash | `` `/api/Problem/${id}` `` |
| GET by relation | `/api/Resource/relation/${id}` | `` `/api/Problem/problembox/${problemBoxId}` `` |
| POST (create) | trailing slash, body = data | `` `/api/Problem/` `` |
| PUT (update) | trailing slash, **id lives in the body**, not the URL | `` `/api/Problem/` `` |
| DELETE | append `${id}` | `` `/api/Problem/${id}` `` |

## WRONG / CORRECT

| WRONG | CORRECT |
|---|---|
| `fetch('http://127.0.0.1:80/api/Problem/')` inside a component | Call `ProblemService.getAll()` from the component; keep the URL in the service |
| `const API_BASE_URL = 'https://127.0.0.1:80'` | `'http://127.0.0.1:80'` — the gateway is plain HTTP. `systemUserService.js:1` uses `https://` and is **broken** — do not copy it |
| `return response` / `return response.body` | `return await response.json()` (or `return true` for delete) |
| Swallowing failures: `if(!response.ok) return null` | `if (!response.ok) throw new Error('Failed to ...')` — pages rely on the throw to set error state |
| `export class ProblemService {}` / default export | `export const ProblemService = { ... }` (named) |
| `import axios` | `fetch` only — axios is not a dependency |

## Auth header — the deliberate gap

Only **2 of 21** services attach a bearer token: `userService.js:3-18` and `anonymousUserService.js:3-18`. Both define a local helper:

```js
const getAuthHeader = () => {
    const token = localStorage.getItem('authToken');
    return token ? { Authorization: `Bearer ${token}` } : {};
};
// ...used as: headers: { ...getAuthHeader() }
```

| WRONG | CORRECT |
|---|---|
| Assuming every service auto-authenticates | 19 of 21 send **no** auth header. If your new endpoint needs the JWT, copy `getAuthHeader()` from `userService.js` and spread it: `headers: { ...getAuthHeader() }` |
| Reading the token inline in a component (`PublicPortal.jsx:168,524,535` does this — don't imitate) | Read `localStorage.getItem('authToken')` inside the service via `getAuthHeader()` |

## Error message parsing (richer tier — 2 services only)

`userService.js` and `anonymousUserService.js` parse the response body instead of a static string:

```js
const extractErrorMessage = async (response) => {
    try {
        const data = await response.json();
        if (data.errors) return Object.values(data.errors).flat().join(' ');
        return data.error || data.title || 'Request failed';
    } catch { return 'Request failed'; }
};
// ...used as: if (!response.ok) throw new Error(await extractErrorMessage(response));
```
Use this richer form for auth/validation endpoints where the server returns field errors (`{errors}`/`{error}`/`{title}`). The other 19 services intentionally throw a fixed `'Failed to ...'` string.

## Adding a new resource service
1. New file `src/services/<resource>Service.js`.
2. First line: `const API_BASE_URL = 'http://127.0.0.1:80';` (http, not https).
3. `export const <Resource>Service = { getAll, getById, create, update, delete }` following the URL table.
4. Every method: `async`, `if (!response.ok) throw`, `return await response.json()`.
5. Add `...getAuthHeader()` only if the endpoint is protected.
