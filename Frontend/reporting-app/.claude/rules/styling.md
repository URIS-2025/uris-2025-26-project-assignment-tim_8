# Rule: styling conventions

Plain CSS only — **no** Tailwind, CSS Modules, or styled-components. Global design tokens + utility classes live in `src/index.css`; each view imports its own sibling `.css`.

## Where styles live
| Layer | File | Holds |
|---|---|---|
| Global tokens | `src/index.css:1-35` | CSS variables (`--bg-primary`, `--accent-primary`, `--danger`, `--radius-md`, `--shadow-lg`, …) |
| Global utilities | `src/index.css` | `.glass-panel` (`:89`), `.glass-nav` (`:98`), `.animate-fade-in` (`:117`), `.delay-100/200/300` |
| Button utilities | `Navbar.css:56-85` | `.btn`, `.btn-ghost`, `.btn-primary` |
| Per-view styles | `PageName.css` next to `PageName.jsx` | layout for that view |

Sibling-CSS import is the norm (`DataTable.jsx:3`, `Modal.jsx:3`, `AdminDashboard.jsx:14`, +17 more). Some pages (`ProblemBoxes.jsx`, `UserManagement.jsx`) ship **no** sibling CSS and rely entirely on global/shared classes — that is acceptable when you reuse utilities.

## WRONG / CORRECT

| WRONG | CORRECT |
|---|---|
| `import 'tailwindcss'` / `className="flex p-4"` | Plain class in a sibling `.css`, or a utility (`glass-panel`, `btn`) |
| Hardcoding colors: `style={{ color: '#ef4444' }}` | `style={{ color: 'var(--danger)' }}` — use the CSS variables |
| A bespoke card class duplicating glass styling | reuse `className="glass-panel"` |
| A new `<button className="my-btn">` with custom CSS | `className="btn btn-ghost"` / `btn btn-primary` / add `icon-btn` for icon-only |
| Static layout written as inline `style={{}}` | put it in `PageName.css` and use `className` |
| A styled page with no CSS file | add `PageName.css` beside `PageName.jsx` and `import './PageName.css'` |

## When inline `style={{}}` IS correct
Inline styles (`style={{` ≈ 303 occurrences across 22 files) are reserved for **runtime-computed values**, not static design:
```jsx
<th style={{ width: col.width || 'auto', textAlign: col.align || 'left' }}>   // DataTable.jsx:81
<span style={{ cursor: 'pointer' }}>                                          // DataTable.jsx:143
<Loader2 style={{ animation: 'spin 1s linear infinite' }} />                  // BoxDetails.jsx:260
```
`StatusBadge.jsx:49-63` is fully inline by design because its colors are computed from `type`/`status`. Don't extend that pattern to ordinary layout.

## Conventions
- Reuse the tokens; the palette is dark/glassmorphism (`--bg-primary: #0f172a`, indigo accent `#6366f1`).
- Compose utilities: `className="data-table-container glass-panel animate-fade-in"` (`DataTable.jsx:48`).
- Icon-only buttons: `className="btn btn-ghost icon-btn small"` (`Modal.jsx:31`).
