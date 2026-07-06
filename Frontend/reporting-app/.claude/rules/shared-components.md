# Rule: shared components (DataTable, Modal, StatusBadge)

Three reusable components live in `src/components/`. Use them instead of hand-rolling tables, dialogs, or status pills. Icons everywhere come from `lucide-react` (24 files).

## DataTable (`components/DataTable.jsx`) — config-driven, used by 6 pages

Pages: `BoxDetails`, `BoxSettings`, `OrganizationDetails`, `ProblemBoxes`, `SuggestionBoxes`, `UserManagement`.

Props (`DataTable.jsx:5-18`):
| Prop | Purpose |
|---|---|
| `title`, `data`, `columns` | required core |
| `onRowClick(row)` | adds `clickable-row`; row click handler |
| `onActionClick(row)` | adds a trailing `⋮` action button column |
| `searchValue`, `onSearchChange` | controlled search box |
| `currentPage`, `onPageChange`, `pageSize=5` | pagination — **active only when `onPageChange` is passed** (`:22`) |
| `searchPlaceholder`, `showExport` | optional UI |

Columns are objects (`DataTable.jsx:80-99`):
```js
const columns = [
  { header: 'Title', accessor: 'title' },                       // plain field
  { header: 'Status', render: (row) => <StatusBadge type="status" status={statusMap[row.status]} /> },
  { header: 'Count', accessor: 'count', align: 'right', width: '80px' },
];
<DataTable title="Submissions" data={items} columns={columns}
           onRowClick={(r) => navigate(`/admin/submissions/${r.id}`)} />
```
Full real example: `UserManagement.jsx:99-215`.

### DataTable paginates CLIENT-side — not for server-paged endpoints
`DataTable` slices the **full** `data` array it is handed (`data.slice((page-1)*pageSize, …)`,
`DataTable.jsx:20-30`) and derives `totalPages` from `data.length`. It only works when the page
already holds the entire dataset. For an endpoint that paginates **server-side** (returns one page +
a total — e.g. `GET /api/Audit` → `{ total, items }`), do NOT pass `onPageChange`: DataTable would
slice an already-partial page and show a wrong total. Instead hand-roll a `<table>` reusing the
existing classes (`data-table-container glass-panel`, `table-responsive`, `custom-table`,
`clickable-row`, `empty-state`, `table-pagination`) and drive Prev/Next from the server `total`.
Evidence: `pages/AuditDashboard.jsx`.

## Modal (`components/Modal.jsx`) — used by 7 pages

Props (`Modal.jsx:5`): `{ isOpen, onClose, title, children, footer }`. Returns `null` when `!isOpen` (`:21`); closes on Escape and on overlay click; locks body scroll (`:7-19`). Renders `glass-panel` + `animate-fade-in`.
```jsx
<Modal isOpen={showEdit} onClose={() => setShowEdit(false)} title="Edit User"
       footer={<button className="btn btn-primary" onClick={save}>Save</button>}>
  {/* form fields as children */}
</Modal>
```
Pages: `AdminDashboard`, `BoxSettings`, `OrganizationDetails`, `ProblemBoxes`, `SubscriptionDetails`, `SuggestionBoxes`, `UserManagement`.

## StatusBadge (`components/StatusBadge.jsx`) — used by 7 files

Props (`StatusBadge.jsx:3`): `{ type, status }`. `type` ∈ `'priority' | 'status' | 'role'`; color is derived from `status.toLowerCase()` (`:5-44`). Fully inline-styled pill (`:49-63`) — no CSS file.
```jsx
<StatusBadge type="status" status="Resolved" />
<StatusBadge type="priority" status="High" />
<StatusBadge type="role" status="Admin" />
```

## WRONG / CORRECT

| WRONG | CORRECT |
|---|---|
| Hand-writing `<table>` markup in a page | `<DataTable columns={...} data={...} />` |
| Inline JSX per cell inside the page render | a `column.render(row)` function in the `columns` config |
| Building a custom modal div + manual Escape listener | `<Modal isOpen onClose title>` |
| Colored `<span>` for status | `<StatusBadge type="status" status={...} />` |
| `import { Icon } from 'react-icons'` / inline SVG | `import { X } from 'lucide-react'` (the only icon lib) |
| Passing `currentPage` without `onPageChange` and expecting paging | pagination needs `onPageChange` (`DataTable.jsx:22`) |
