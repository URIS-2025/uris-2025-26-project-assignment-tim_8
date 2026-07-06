import {
    initialChatState, applyResponse, buildMessageRequest, buildConfirmRequest,
} from './chatSession';

// The server is stateless: it echoes the whole conversation back (camelCase on the wire) and the
// client replays it on the next call. On a proposed write the server returns status
// "pending_confirmation" + a proposal; the client must reply with pendingConfirmation carrying the
// SAME toolUseId. CorrelationId ties the whole logical interaction to one audit chain.

const completed = {
    status: 'completed',
    assistantText: 'You have 3 active boxes.',
    proposal: null,
    history: [{ role: 'user', content: [{ type: 'text', text: 'how many boxes?' }] }],
    correlationId: 'corr-1',
    iterations: 1,
};

const pending = {
    status: 'pending_confirmation',
    assistantText: null,
    proposal: { toolUseId: 'tu-42', toolName: 'set_box_status', argsSummary: 'id=<len:36> status=1' },
    history: [{ role: 'assistant', content: [{ type: 'tool_use', toolUseId: 'tu-42' }] }],
    correlationId: 'corr-9',
    iterations: 2,
};

describe('initialChatState', () => {
    test('empty history, no correlation, no pending', () => {
        expect(initialChatState()).toEqual({ history: [], correlationId: null, pending: null });
    });
});

describe('applyResponse', () => {
    test('completed → adopts history+correlationId, clears pending', () => {
        const s = applyResponse(initialChatState(), completed);
        expect(s.history).toBe(completed.history);
        expect(s.correlationId).toBe('corr-1');
        expect(s.pending).toBeNull();
    });

    test('pending_confirmation → captures the proposal as pending', () => {
        const s = applyResponse(initialChatState(), pending);
        expect(s.pending).toEqual({
            toolUseId: 'tu-42', toolName: 'set_box_status', argsSummary: 'id=<len:36> status=1',
        });
        expect(s.correlationId).toBe('corr-9');
    });

    test('a completed response CLEARS a previously pending proposal', () => {
        const s1 = applyResponse(initialChatState(), pending);
        expect(s1.pending).not.toBeNull();
        const s2 = applyResponse(s1, completed);
        expect(s2.pending).toBeNull();
    });
});

describe('buildMessageRequest', () => {
    test('carries message + echoed history + correlationId', () => {
        const s = applyResponse(initialChatState(), completed);
        const req = buildMessageRequest(s, 'and how many suggestions?');
        expect(req).toEqual({
            message: 'and how many suggestions?',
            history: completed.history,
            correlationId: 'corr-1',
        });
    });
});

describe('buildConfirmRequest', () => {
    test('carries the SAME toolUseId + approval + history + correlationId, no message', () => {
        const s = applyResponse(initialChatState(), pending);
        const req = buildConfirmRequest(s, true);
        expect(req).toEqual({
            history: pending.history,
            pendingConfirmation: { toolUseId: 'tu-42', approved: true },
            correlationId: 'corr-9',
        });
        expect(req.message).toBeUndefined();
    });

    test('reject carries approved:false', () => {
        const s = applyResponse(initialChatState(), pending);
        expect(buildConfirmRequest(s, false).pendingConfirmation.approved).toBe(false);
    });

    test('throws when there is no pending proposal to confirm', () => {
        const s = applyResponse(initialChatState(), completed);
        expect(() => buildConfirmRequest(s, true)).toThrow();
    });
});
