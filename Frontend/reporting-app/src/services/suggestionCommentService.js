const API_BASE_URL = 'http://127.0.0.1:80';

export const SuggestionCommentService = {
    // GET /api/SuggestionComment
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/SuggestionComment/`);
        if (!response.ok) throw new Error('Failed to fetch suggestion comments');
        return await response.json();
    },

    // GET /api/SuggestionComment/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/SuggestionComment/${id}`);
        if (!response.ok) throw new Error('Failed to fetch suggestion comment');
        return await response.json();
    },

    // GET /api/SuggestionComment/suggestion/{id}
    getBySuggestionId: async (suggestionId) => {
        const response = await fetch(`${API_BASE_URL}/api/SuggestionComment/suggestion/${suggestionId}`);
        if (!response.ok) throw new Error('Failed to fetch comments by suggestion');
        return await response.json();
    },

    // POST /api/SuggestionComment
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/SuggestionComment/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to create suggestion comment');
        return await response.json();
    },

    // PUT /api/SuggestionComment
    update: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/SuggestionComment/`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to update suggestion comment');
        return await response.json();
    },

    // DELETE /api/SuggestionComment/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/SuggestionComment/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete suggestion comment');
        return true;
    }
};
