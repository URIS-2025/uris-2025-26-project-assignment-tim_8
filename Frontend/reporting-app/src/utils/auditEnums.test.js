import {
    decisionLabel, outcomeLabel, confirmationLabel,
    decisionTone, outcomeTone, toneColor,
} from './auditEnums';

// The gateway serializes enums as INT on the wire (no JsonStringEnumConverter):
//   Decision {Allow=0, Deny=1}, Outcome {Success=0, Error=1, NotExecuted=2},
//   Confirmation {Proposed=0, Confirmed=1, Rejected=2}.

describe('decisionLabel', () => {
    test('maps the two decision codes', () => {
        expect(decisionLabel(0)).toBe('Allow');
        expect(decisionLabel(1)).toBe('Deny');
    });
    test('unknown code → "Unknown"', () => {
        expect(decisionLabel(9)).toBe('Unknown');
        expect(decisionLabel(null)).toBe('Unknown');
        expect(decisionLabel(undefined)).toBe('Unknown');
    });
});

describe('outcomeLabel', () => {
    test('maps the three outcome codes', () => {
        expect(outcomeLabel(0)).toBe('Success');
        expect(outcomeLabel(1)).toBe('Error');
        expect(outcomeLabel(2)).toBe('NotExecuted');
    });
    test('unknown code → "Unknown"', () => {
        expect(outcomeLabel(7)).toBe('Unknown');
    });
});

describe('confirmationLabel', () => {
    test('maps the three confirmation codes', () => {
        expect(confirmationLabel(0)).toBe('Proposed');
        expect(confirmationLabel(1)).toBe('Confirmed');
        expect(confirmationLabel(2)).toBe('Rejected');
    });
    test('null / undefined → null (no confirmation on this record)', () => {
        expect(confirmationLabel(null)).toBeNull();
        expect(confirmationLabel(undefined)).toBeNull();
    });
    test('unknown code → "Unknown"', () => {
        expect(confirmationLabel(5)).toBe('Unknown');
    });
});

describe('tone helpers (semantic color, not raw hex)', () => {
    test('decisionTone: Allow→success, Deny→danger', () => {
        expect(decisionTone(0)).toBe('success');
        expect(decisionTone(1)).toBe('danger');
    });
    test('outcomeTone: Success→success, Error→danger, NotExecuted→muted', () => {
        expect(outcomeTone(0)).toBe('success');
        expect(outcomeTone(1)).toBe('danger');
        expect(outcomeTone(2)).toBe('muted');
    });
    test('toneColor maps a tone to a CSS variable', () => {
        expect(toneColor('success')).toBe('var(--success)');
        expect(toneColor('danger')).toBe('var(--danger)');
        expect(toneColor('muted')).toBe('var(--text-secondary)');
        expect(toneColor('anything-else')).toBe('var(--text-secondary)');
    });
});
