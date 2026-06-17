# Rule: forms & client-side validation

No form library (no Formik / react-hook-form / yup). Forms are a native `<form onSubmit>` with a manual handler that validates with guard clauses, then calls a service. 5 `onSubmit={handleSubmit}` forms; 67 `onChange={` across 14 files.

## The submit-handler contract (`AnonymousSignup.jsx:25-56`)

```js
const [isSubmitting, setIsSubmitting] = useState(false);
const [error, setError] = useState('');

const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');                                   // 1. clear prior error

    const username = e.target.username.value.trim();
    if (!username || !password) { setError('Please fill in all fields.'); return; }   // 2. guard clauses
    if (password.length < 8) { setError('Password must be at least 8 characters.'); return; }
    if (password !== confirmPassword) { setError('Passwords do not match.'); return; }

    try {
        setIsSubmitting(true);                      // 3. disable button
        await AnonymousUserService.create({ username, password });   // 4. service call
        navigate('/anonymous/login');               // 5. navigate on success
    } catch (err) {
        setError(err.message || 'Failed to create anonymous account.');   // 6. surface server error
    } finally {
        setIsSubmitting(false);
    }
};
```
Real messages: `'Password must be at least 8 characters.'` (`AnonymousSignup.jsx:38`), `'Passwords do not match.'` (`:43`), `'Please select a role.'` (`Login.jsx:123`).

## Inputs
Two input styles coexist:
- **Controlled** via `useState` (preferred for fields you read during typing, e.g. live password strength): `value={password} onChange={e => setPassword(e.target.value)}` (`AnonymousSignup.jsx:21`).
- **Object-form state** for multi-field forms: `const [formData, setFormData] = useState({title,content,boxId})` with `onChange={e => setFormData(prev => ({...prev, title: e.target.value}))}` (`AnonymousSubmit.jsx:25-29`).
- Some fields are read at submit time via `e.target.<name>.value` with `name=` + `required` (`AnonymousSignup.jsx:29,93`) rather than controlled state — acceptable for write-only fields.

## Error display
Inline error block with a lucide `AlertCircle`, conditionally rendered (`AnonymousSignup.jsx:75-85`, `Login.jsx:174-184`):
```jsx
{error && (
  <div style={{ background: 'rgba(239,68,68,0.1)', border: '1px solid rgba(239,68,68,0.3)', color: '#fca5a5' }}>
    <AlertCircle size={16} /> <span>{error}</span>
  </div>
)}
```

## WRONG / CORRECT

| WRONG | CORRECT |
|---|---|
| Adding Formik/react-hook-form/yup | native `<form onSubmit={handleSubmit}>` + manual validation |
| Validating in an `onChange` per field | guard clauses at the top of `handleSubmit`, early `return` on failure |
| Forgetting `e.preventDefault()` | first line of the handler |
| Not resetting the error before re-submit | `setError('')` before validating |
| `alert('Passwords do not match')` | inline `{error && <div>…<AlertCircle/></div>}` — **`AnonymousLogin.jsx:20,44` uses `alert()`; that is the outlier, don't copy it** |
| Ignoring the thrown service error | `catch (err) { setError(err.message) }` (the service already threw a useful message) |
| Leaving the submit button enabled mid-request | gate on `isSubmitting` set in `try`/cleared in `finally` |
