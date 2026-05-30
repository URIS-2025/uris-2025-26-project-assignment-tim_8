# Plan: Bootstrap knowledge base (.claude/rules + .claude/guides + CLAUDE.md) for the React frontend

Base branch: dev
Mode: local
Created: 2026-05-30

## Scope / path conventions

All deliverables live under the **frontend app root**: `Frontend/reporting-app/`.
- Rules → `Frontend/reporting-app/.claude/rules/*.md`
- Guides → `Frontend/reporting-app/.claude/guides/*.md`
- Index → `Frontend/reporting-app/CLAUDE.md`

A backend-oriented `CLAUDE.md` already exists at the **repo** root
(`uris-2025-26-project-assignment-tim_8/CLAUDE.md`) — do NOT overwrite it. The new
`CLAUDE.md` is frontend-scoped and lives in the app dir, which is the working
directory agents are launched in for frontend work.

All grep/path evidence in task files is repo-relative to `Frontend/reporting-app/`
unless prefixed otherwise. Skip `node_modules/`, `build/`, lockfiles.

## Tasks

| #   | Name                       | Type     | Dependencies |
|-----|----------------------------|----------|--------------|
| 001 | inventory-scan-report      | research | —            |
| 002 | rule-services-api          | code     | 001          |
| 003 | rule-data-fetching-state   | code     | 001          |
| 004 | rule-routing-guards        | code     | 001          |
| 005 | rule-auth-context          | code     | 001          |
| 006 | rule-shared-components     | code     | 001          |
| 007 | rule-styling               | code     | 001          |
| 008 | rule-forms-validation      | code     | 001          |
| 009 | guide-architecture-overview| research | 001          |
| 010 | guide-auth-flow            | research | 001          |
| 011 | guide-api-contract-mapping | research | 001          |
| 012 | guide-admin-dashboard      | research | 001          |
| 013 | guide-anonymous-flow       | research | 001          |
| 014 | index-claude-md            | code     | 002,003,004,005,006,007,008,009,010,011,012,013 |

## Acceptance criteria

- `.claude/rules/*.md`: each ≤500 lines, table-heavy, WRONG/CORRECT pairs, real
  file/component/hook names, ≥3 grep occurrences cited as evidence per rule file.
- `.claude/guides/*.md`: each lists files actually read; covers entry points, key
  entities, data flow, extension points, gotchas, file map.
- `CLAUDE.md`: project description, build/test/dev commands, architecture table,
  rules table, guides table, constraints section.
- No invented patterns. Only document what exists (3+ occurrences). Tables over prose.
  Repo-relative paths only.

## Stack facts (already verified by planner scan — start here)

- React 19.2, react-router-dom 7.13, react-scripts 5 (CRA), jwt-decode 4, lucide-react.
- NO Redux/Zustand/React Query/Tailwind/styled-components/form library.
- State: local `useState` + single `src/context/AuthContext.js`.
- Data: 21 service modules in `src/services/*.js`, each a plain exported object
  (`export const XService = { getAll, getById, create, update, delete }`), all hardcode
  `const API_BASE_URL = 'http://127.0.0.1:80'`, raw `fetch`, `if (!response.ok) throw new Error(...)`.
  Only `anonymousUserService.js` and one other send `getAuthHeader()`.
- Routing: all in `src/App.js`; `ProtectedRoute` wrapper; `Outlet` layouts; `useNavigate` in 14 files.
- Styling: per-view plain `.css` files (13 page CSS + component CSS); shared utility classes
  `glass-panel`, `btn`, `btn-ghost`, `icon-btn`, `animate-fade-in`; ~22 inline `style={{}}` spots.
- Shared components: `DataTable` (config-driven `columns`, 6 pages), `Modal` (7 pages),
  `StatusBadge`, `Navbar`, `Sidebar`.
