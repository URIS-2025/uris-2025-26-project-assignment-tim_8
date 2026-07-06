import { AuditService } from './auditService';

describe('AuditService', () => {
    beforeEach(() => {
        localStorage.clear();
        global.fetch = jest.fn(() =>
            Promise.resolve({ ok: true, json: () => Promise.resolve({ total: 0, items: [] }) })
        );
    });
    afterEach(() => jest.restoreAllMocks());

    test('getAudit attaches the bearer from authToken', async () => {
        localStorage.setItem('authToken', 'tok-audit');
        await AuditService.getAudit({}, { isAdmin: true });
        const [url, options] = global.fetch.mock.calls[0];
        expect(url).toContain('/api/Audit/?');
        expect(options.headers.Authorization).toBe('Bearer tok-audit');
    });

    test('a manager request does not carry organizationId in the URL', async () => {
        localStorage.setItem('authToken', 'tok');
        await AuditService.getAudit({ organizationId: 'org-x', toolName: 'list_boxes' }, { isAdmin: false });
        const [url] = global.fetch.mock.calls[0];
        expect(url).not.toContain('organizationId');
        expect(url).toContain('toolName=list_boxes');
    });

    test('throws the server error message on !ok', async () => {
        global.fetch = jest.fn(() =>
            Promise.resolve({ ok: false, json: () => Promise.resolve({ error: 'Forbidden' }) })
        );
        await expect(AuditService.getAudit({}, { isAdmin: true })).rejects.toThrow('Forbidden');
    });
});
