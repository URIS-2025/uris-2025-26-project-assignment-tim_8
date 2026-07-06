const API_BASE_URL = 'http://127.0.0.1:80';

// POST /api/AiChat is [Authorize] — the caller's JWT is forwarded to the gateway as the OBO principal.
const getAuthHeader = () => {
    const token = localStorage.getItem('authToken');
    return token ? { Authorization: `Bearer ${token}` } : {};
};

const extractErrorMessage = async (response) => {
    try {
        const data = await response.json();
        // The agent controller fails closed with { error } (400) and the rate limiter with { error } (429).
        return data.error || data.message || 'Asistent trenutno ne moze da obradi zahtev.';
    } catch {
        return 'Asistent trenutno ne moze da obradi zahtev.';
    }
};

export const AiChatService = {
    // body is built by utils/chatSession (buildMessageRequest / buildConfirmRequest).
    send: async (body) => {
        const response = await fetch(`${API_BASE_URL}/api/AiChat/`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', ...getAuthHeader() },
            body: JSON.stringify(body),
        });
        if (!response.ok) throw new Error(await extractErrorMessage(response));
        return response.json();
    },
};
