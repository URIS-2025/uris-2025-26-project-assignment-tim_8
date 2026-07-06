import { AiChatService } from './aiChatService';

describe('AiChatService', () => {
    beforeEach(() => {
        localStorage.clear();
        global.fetch = jest.fn(() =>
            Promise.resolve({ ok: true, json: () => Promise.resolve({ status: 'completed', history: [] }) })
        );
    });
    afterEach(() => jest.restoreAllMocks());

    test('send POSTs the body with the bearer + JSON content type', async () => {
        localStorage.setItem('authToken', 'tok-chat');
        await AiChatService.send({ message: 'hi', history: [], correlationId: null });
        const [url, options] = global.fetch.mock.calls[0];
        expect(url).toBe('http://127.0.0.1:80/api/AiChat/');
        expect(options.method).toBe('POST');
        expect(options.headers.Authorization).toBe('Bearer tok-chat');
        expect(options.headers['Content-Type']).toBe('application/json');
        expect(JSON.parse(options.body)).toEqual({ message: 'hi', history: [], correlationId: null });
    });

    test('throws the fixed agent error on !ok (fail-closed { error })', async () => {
        global.fetch = jest.fn(() =>
            Promise.resolve({ ok: false, json: () => Promise.resolve({ error: 'Previse zahteva.' }) })
        );
        await expect(AiChatService.send({ message: 'x' })).rejects.toThrow('Previse zahteva.');
    });
});
