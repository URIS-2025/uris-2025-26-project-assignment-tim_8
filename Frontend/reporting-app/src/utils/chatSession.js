// Pure state machine for the AiChat propose-confirm flow (no React — unit-testable below the router).
//
// The server is stateless: every response echoes the full conversation (`history`) + a `correlationId`
// that ties one logical interaction to its audit chain. When the agent wants to run a WRITE it returns
// status "pending_confirmation" + a `proposal`; the client must reply with the SAME toolUseId and an
// approve/reject decision. Wire keys are camelCase (ASP.NET web-default serialization).

export const initialChatState = () => ({ history: [], correlationId: null, pending: null });

// Fold a server response into the session. A "pending_confirmation" captures the proposal; any other
// status (e.g. "completed") clears it — so a confirmed/rejected write never leaves a stale proposal.
export const applyResponse = (state, response) => ({
    ...state,
    history: response.history ?? [],
    correlationId: response.correlationId ?? null,
    pending: response.status === 'pending_confirmation' && response.proposal
        ? {
            toolUseId: response.proposal.toolUseId,
            toolName: response.proposal.toolName,
            argsSummary: response.proposal.argsSummary,
        }
        : null,
});

// A new user turn: message + the echoed history + the running correlationId.
export const buildMessageRequest = (state, message) => ({
    message,
    history: state.history,
    correlationId: state.correlationId,
});

// A decision on the pending write. Carries the exact proposed toolUseId so the server matches it to
// the proposal it made; no `message` (this turn only confirms). Throws if nothing is pending.
export const buildConfirmRequest = (state, approved) => {
    if (!state.pending) {
        throw new Error('Nema predloga za potvrdu (state.pending je prazan).');
    }
    return {
        history: state.history,
        pendingConfirmation: { toolUseId: state.pending.toolUseId, approved },
        correlationId: state.correlationId,
    };
};
