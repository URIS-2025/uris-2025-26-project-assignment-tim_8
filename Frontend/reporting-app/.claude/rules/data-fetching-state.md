# Rule: data fetching & component state

Pages fetch their own data with local `useState` + `useEffect`. There is **no** global store, no React Query, no SWR. Server data lives in the page that shows it.

## Canonical lifecycle (`BoxDetails.jsx:40-96`)

```js
const [box, setBox]       = useState(null);
const [items, setItems]   = useState([]);
const [loading, setLoading] = useState(true);   // start TRUE
const [error, setError]   = useState(null);

useEffect(() => {
    fetchBoxData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
}, [boxId]);

const fetchBoxData = async () => {
    try {
        setLoading(true);
        setError(null);
        const data = await SuggestionBoxService.getById(boxId);   // service call
        setBox(data);
    } catch (err) {
        console.error('Error fetching box data:', err);
        setError(err.message || 'Failed to load ...');
    } finally {
        setLoading(false);
    }
};
```
Evidence: `useEffect` in 13 pages; `} finally {` 32 occurrences (`BoxDetails.jsx:93`, `ProblemBoxes.jsx:54,87`, `OrganizationDetails.jsx:120,132,154`, `SubmissionDetails.jsx:109,212`). Initial `useState(true)` loading: `BoxDetails.jsx:43`, `AdminDashboard.jsx:32` (`isLoading`), `BillingDashboard.jsx:31`.

## Early-return UI (`BoxDetails.jsx:256-281`)

```jsx
if (loading) {
    return (<div className="... animate-fade-in" style={{ ... }}>
        <Loader2 size={32} style={{ animation: 'spin 1s linear infinite' }} />
        <p>Loading box details...</p>
    </div>);
}
if (error) {
    return (<div className="glass-panel" style={{ color: 'var(--danger)' }}>
        <p>{error}</p>
        <button className="btn btn-ghost" onClick={fetchBoxData}>Retry</button>
    </div>);
}
// ...main render uses box/items, guaranteed loaded & error-free
```
Same loading-spinner + error-panel-with-Retry shape in `ProblemBoxes.jsx:202-208`.

## WRONG / CORRECT

| WRONG | CORRECT |
|---|---|
| `fetch`/service call in the component render body | Define an `async` fn and call it inside `useEffect` |
| `useState(false)` for loading then never showing a spinner | `useState(true)`; render an early-return spinner while loading |
| Clearing `setLoading(false)` only in the `try` | Clear it in `finally` so errors also stop the spinner |
| Letting a thrown service error bubble uncaught | `catch (err) { setError(err.message) }` and render the error branch |
| Putting fetched lists in `AuthContext` or a global | Keep server data in the page's local `useState` |
| Rendering main JSX that dereferences `box.foo` before load | Early-return on `loading`/`error` first, so main render is safe |
| `useEffect(() => { fetch... })` with no dependency array | Pass deps (`[boxId]`) so it refetches on param change, runs once otherwise |

## Notes
- Refetch is triggered by putting the route param in the dep array (`[boxId]`), with `// eslint-disable-next-line react-hooks/exhaustive-deps` above it (the project's accepted convention).
- The retry button re-invokes the same fetch function (`onClick={fetchBoxData}`).
- Numeric enums from the API are mapped client-side to labels (`statusMap`/`priorityMap` in `BoxDetails.jsx:13-27`) — see `guides/api-contract-mapping.md`.
