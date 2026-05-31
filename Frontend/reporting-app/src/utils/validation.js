// Shared client-side validation utilities.
// IMPORTANT: messages and regex below are part of a shared contract with the
// backend — they must stay VERBATIM identical (including punctuation/en-dash).

const COMPLEXITY_REGEX = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,64}$/;
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const USERNAME_REGEX = /^[a-zA-Z0-9_]+$/;

const MESSAGES = {
    passwordTooShort: 'Password must be at least 8 characters.',
    passwordTooLong: 'Password must be at most 64 characters.',
    passwordComplexity: 'Password must include uppercase, lowercase, a number, and a special character.',
    passwordBreached: 'This password has appeared in a data breach. Please choose another.',
    email: 'Please enter a valid email address.',
    username: 'Username may only contain letters, digits, and underscores (3–30 chars).',
};

export const validateEmail = (email) => {
    const normalized = String(email ?? '').toLowerCase().trim();
    if (!EMAIL_REGEX.test(normalized)) {
        return { valid: false, message: MESSAGES.email };
    }
    return { valid: true, message: '' };
};

export const validatePassword = (password) => {
    const value = String(password ?? '');
    if (value.length < 8) {
        return { valid: false, message: MESSAGES.passwordTooShort };
    }
    if (value.length > 64) {
        return { valid: false, message: MESSAGES.passwordTooLong };
    }
    if (!COMPLEXITY_REGEX.test(value)) {
        return { valid: false, message: MESSAGES.passwordComplexity };
    }
    return { valid: true, message: '' };
};

export const validateUsername = (username) => {
    const value = String(username ?? '');
    if (!USERNAME_REGEX.test(value) || value.length < 3 || value.length > 30) {
        return { valid: false, message: MESSAGES.username };
    }
    return { valid: true, message: '' };
};
