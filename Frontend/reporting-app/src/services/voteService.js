const API_BASE_URL = 'http://127.0.0.1:80';

export const VoteService = {
    // GET /api/Vote
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/Vote/`);
        if (!response.ok) throw new Error('Failed to fetch votes');
        return await response.json();
    },

    // GET /api/Vote/suggestion/{id}
    getBySuggestionId: async (suggestionId) => {
        const response = await fetch(`${API_BASE_URL}/api/Vote/suggestion/${suggestionId}`);
        if (!response.ok) throw new Error('Failed to fetch votes by suggestion');
        return await response.json();
    },

    // POST /api/Vote
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/Vote/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to create vote');
        return await response.json();
    },

    // DELETE /api/Vote/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/Vote/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete vote');
        return true;
    }
};
