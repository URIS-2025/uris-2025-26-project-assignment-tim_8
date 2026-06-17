# Frontend Testing (CRA 5 + Jest)

The frontend (`Frontend/reporting-app`) is Create React App 5 (`react-scripts test`,
Jest + jsdom). One hard constraint shapes what is testable.

## Jest cannot resolve `react-router-dom` v7 — test below the router

`react-router-dom` v7 ships ESM-only with an `exports` map that react-scripts 5's bundled
Jest resolver does not understand. Any test that imports the router — or imports a module
that transitively imports it, e.g. `App.js` — fails at collection time:

```
Cannot find module 'react-router-dom' from 'src/App.test.js'
```

| Pattern | Evidence |
|---------|----------|
| WRONG: import `App.js` / `react-router-dom` into a Jest test | fails to resolve, 0 tests run |
| CORRECT: test the logic one layer below the router | `src/context/AuthContext.test.js` |

```jsx
// WRONG — pulls in react-router-dom, suite fails to load
import { ProtectedRoute } from './App';

// CORRECT — test the context/service the route guard depends on, no router import
import { AuthProvider, useAuth } from './context/AuthContext';
```

So: put behavior in a context/service/hook and test that; keep route guards thin. Changing
the Jest config (a `moduleNameMapper` for `react-router-dom`, or CRACO) is the only way to
test router-level components, and is out of scope unless explicitly requested.

## Run a single suite without watch mode

```bash
CI=true npx react-scripts test <path> --watchAll=false
```

`CI=true` also turns lint warnings into a failed `npm run build`; the repo has pre-existing
warnings, so use a plain `npx react-scripts build` ("Compiled with warnings") to check your
own files compile.
