import { OrganizationService } from './organizationService';

describe('OrganizationService auth headers', () => {
    beforeEach(() => {
        localStorage.clear();
        global.fetch = jest.fn(() =>
            Promise.resolve({ ok: true, json: () => Promise.resolve({}) })
        );
    });

    afterEach(() => {
        jest.restoreAllMocks();
    });

    test('create attaches Authorization header from authToken', async () => {
        localStorage.setItem('authToken', 'test-token-123');

        await OrganizationService.create({ name: 'Acme' });

        expect(global.fetch).toHaveBeenCalledTimes(1);
        const [, options] = global.fetch.mock.calls[0];
        expect(options.headers.Authorization).toBe('Bearer test-token-123');
        expect(options.headers['Content-Type']).toBe('application/json');
    });

    test('update attaches Authorization header from authToken', async () => {
        localStorage.setItem('authToken', 'tok');

        await OrganizationService.update({ id: '1', name: 'Acme' });

        const [, options] = global.fetch.mock.calls[0];
        expect(options.headers.Authorization).toBe('Bearer tok');
    });

    test('delete attaches Authorization header from authToken', async () => {
        localStorage.setItem('authToken', 'tok');

        await OrganizationService.delete('1');

        const [, options] = global.fetch.mock.calls[0];
        expect(options.headers.Authorization).toBe('Bearer tok');
    });

    test('create omits Authorization when no token is present', async () => {
        await OrganizationService.create({ name: 'Acme' });

        const [, options] = global.fetch.mock.calls[0];
        expect(options.headers.Authorization).toBeUndefined();
    });
});
