// Audit enum → label / tone mapping.
//
// The gateway (McpGateway) has NO JsonStringEnumConverter, so GET /api/Audit serializes its enums
// as INT on the wire. These maps turn those integer codes into display labels + a semantic "tone"
// (never a raw hex — the tone maps to a CSS variable so the palette stays centralized).
//   Decision     {Allow=0, Deny=1}
//   Outcome      {Success=0, Error=1, NotExecuted=2}
//   Confirmation {Proposed=0, Confirmed=1, Rejected=2}  (nullable — no confirmation on read tools)

const DECISION = { 0: 'Allow', 1: 'Deny' };
const OUTCOME = { 0: 'Success', 1: 'Error', 2: 'NotExecuted' };
const CONFIRMATION = { 0: 'Proposed', 1: 'Confirmed', 2: 'Rejected' };

export const decisionLabel = (code) => DECISION[code] ?? 'Unknown';
export const outcomeLabel = (code) => OUTCOME[code] ?? 'Unknown';

// Confirmation is nullable: a null/undefined code means "no confirmation applies" (→ null, so the
// UI renders nothing), whereas an out-of-range code is a genuine "Unknown".
export const confirmationLabel = (code) => {
    if (code === null || code === undefined) return null;
    return CONFIRMATION[code] ?? 'Unknown';
};

export const decisionTone = (code) => (code === 0 ? 'success' : 'danger');

export const outcomeTone = (code) => {
    if (code === 0) return 'success';
    if (code === 1) return 'danger';
    return 'muted'; // NotExecuted (and anything else) is neutral, not an error
};

const TONE_COLOR = {
    success: 'var(--success)',
    danger: 'var(--danger)',
    warning: 'var(--warning)',
    muted: 'var(--text-secondary)',
};

export const toneColor = (tone) => TONE_COLOR[tone] ?? 'var(--text-secondary)';
