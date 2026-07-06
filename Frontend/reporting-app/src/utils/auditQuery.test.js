import { buildAuditQuery } from './auditQuery';

const parse = (qs) => Object.fromEntries(new URLSearchParams(qs));

describe('buildAuditQuery', () => {
    test('empty filters → only page + pageSize defaults', () => {
        const p = parse(buildAuditQuery({}, { isAdmin: true }));
        expect(p).toEqual({ page: '0', pageSize: '50' });
    });

    test('omits empty string / null / undefined values', () => {
        const p = parse(buildAuditQuery(
            { toolName: '', agentId: null, userId: undefined, decision: 1 },
            { isAdmin: true }));
        expect(p.toolName).toBeUndefined();
        expect(p.agentId).toBeUndefined();
        expect(p.userId).toBeUndefined();
        expect(p.decision).toBe('1');
    });

    test('page 0 is KEPT (0 is a valid value, not "empty")', () => {
        const p = parse(buildAuditQuery({ page: 0, pageSize: 25 }, { isAdmin: true }));
        expect(p.page).toBe('0');
        expect(p.pageSize).toBe('25');
    });

    test('full filter set for an admin includes organizationId + INT enums', () => {
        const p = parse(buildAuditQuery({
            toolName: 'list_boxes', agentId: 'ai-assistant', userId: 'u1',
            decision: 1, outcome: 0, organizationId: 'org-9',
            from: '2026-07-01T00:00:00Z', to: '2026-07-06T00:00:00Z',
            page: 2, pageSize: 20,
        }, { isAdmin: true }));
        expect(p).toEqual({
            toolName: 'list_boxes', agentId: 'ai-assistant', userId: 'u1',
            decision: '1', outcome: '0', organizationId: 'org-9',
            from: '2026-07-01T00:00:00Z', to: '2026-07-06T00:00:00Z',
            page: '2', pageSize: '20',
        });
    });

    test('a non-admin NEVER sends organizationId (backend force-scopes it)', () => {
        const p = parse(buildAuditQuery(
            { organizationId: 'org-i-should-not-widen-to', toolName: 'get_org_overview' },
            { isAdmin: false }));
        expect(p.organizationId).toBeUndefined();
        expect(p.toolName).toBe('get_org_overview');
    });

    test('decision/outcome 0 (Allow/Success) are KEPT, not dropped as falsy', () => {
        const p = parse(buildAuditQuery({ decision: 0, outcome: 0 }, { isAdmin: true }));
        expect(p.decision).toBe('0');
        expect(p.outcome).toBe('0');
    });
});
