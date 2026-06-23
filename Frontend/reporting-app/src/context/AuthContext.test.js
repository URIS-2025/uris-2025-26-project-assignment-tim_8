import { render, screen, waitFor } from '@testing-library/react';
import { AuthProvider, useAuth } from './AuthContext';

// Build a syntactically valid (unsigned) JWT so jwt-decode can read the exp claim.
const makeToken = (payload) => {
    const b64 = (o) =>
        btoa(JSON.stringify(o)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
    return `${b64({ alg: 'HS256', typ: 'JWT' })}.${b64(payload)}.sig`;
};

let initializingHistory;

const Probe = () => {
    const { user, initializing } = useAuth();
    initializingHistory.push(initializing);
    return (
        <div>
            <div data-testid="state">
                {initializing ? 'init' : user ? `user:${user.role}` : 'anon'}
            </div>
            <div data-testid="org">{user?.organizationId ?? 'none'}</div>
        </div>
    );
};

describe('AuthProvider rehydration', () => {
    beforeEach(() => {
        localStorage.clear();
        initializingHistory = [];
    });

    test('starts in an initializing state on first render', async () => {
        render(
            <AuthProvider>
                <Probe />
            </AuthProvider>
        );

        await waitFor(() =>
            expect(screen.getByTestId('state')).not.toHaveTextContent('init')
        );
        // The fix: ProtectedRoute must be able to tell "still loading" from "logged out".
        expect(initializingHistory[0]).toBe(true);
    });

    test('restores a valid session from localStorage after initializing', async () => {
        const token = makeToken({ exp: Math.floor(Date.now() / 1000) + 3600 });
        localStorage.setItem('authToken', token);
        localStorage.setItem(
            'authUser',
            JSON.stringify({ id: '1', email: 'a@b.c', role: 'admin', token })
        );

        render(
            <AuthProvider>
                <Probe />
            </AuthProvider>
        );

        await waitFor(() =>
            expect(screen.getByTestId('state')).toHaveTextContent('user:admin')
        );
    });

    test('finishes initializing with no user when storage is empty', async () => {
        render(
            <AuthProvider>
                <Probe />
            </AuthProvider>
        );

        await waitFor(() => expect(screen.getByTestId('state')).toHaveTextContent('anon'));
    });

    test('extracts organizationId from the JWT on rehydration (self-heals a stale session)', async () => {
        const token = makeToken({
            exp: Math.floor(Date.now() / 1000) + 3600,
            OrganizationId: 'org-123',
        });
        localStorage.setItem('authToken', token);
        // Stale stored user predates this feature: it has a role but no organizationId.
        localStorage.setItem(
            'authUser',
            JSON.stringify({ id: '1', email: 'm@b.c', role: 'manager', token })
        );

        render(
            <AuthProvider>
                <Probe />
            </AuthProvider>
        );

        await waitFor(() =>
            expect(screen.getByTestId('state')).toHaveTextContent('user:manager')
        );
        expect(screen.getByTestId('org')).toHaveTextContent('org-123');
    });

    test('maps an empty OrganizationId claim to null (admin without an org)', async () => {
        const token = makeToken({
            exp: Math.floor(Date.now() / 1000) + 3600,
            OrganizationId: '',
        });
        localStorage.setItem('authToken', token);
        localStorage.setItem(
            'authUser',
            JSON.stringify({ id: '1', email: 'a@b.c', role: 'admin', token })
        );

        render(
            <AuthProvider>
                <Probe />
            </AuthProvider>
        );

        await waitFor(() =>
            expect(screen.getByTestId('state')).toHaveTextContent('user:admin')
        );
        expect(screen.getByTestId('org')).toHaveTextContent('none');
    });
});
