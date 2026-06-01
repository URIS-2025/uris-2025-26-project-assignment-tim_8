// Soft "have I been pwned" check using the k-anonymity range API.
// This intentionally calls HIBP directly (NOT the app gateway / API_BASE_URL).
// Fails OPEN: on any error it returns 'unknown' so it never blocks the user.

export const checkPwned = async (password) => {
    try {
        const hashBuf = await crypto.subtle.digest('SHA-1', new TextEncoder().encode(password));
        const hashHex = Array.from(new Uint8Array(hashBuf))
            .map((b) => b.toString(16).padStart(2, '0'))
            .join('')
            .toUpperCase();

        const prefix = hashHex.slice(0, 5);
        const suffix = hashHex.slice(5);

        const response = await fetch('https://api.pwnedpasswords.com/range/' + prefix);
        if (!response.ok) {
            return 'unknown';
        }

        const text = await response.text();
        const lines = text.split('\n');
        for (const line of lines) {
            const lineSuffix = line.split(':')[0].trim();
            if (lineSuffix === suffix) {
                return 'breached';
            }
        }
        return 'safe';
    } catch {
        return 'unknown';
    }
};
