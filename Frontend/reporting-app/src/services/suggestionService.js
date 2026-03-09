const API_BASE_URL = 'http://127.0.0.1:80';

export const SuggestionService = {
    // GET /api/Suggestion
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/Suggestion/`);
        if (!response.ok) throw new Error('Failed to fetch suggestions');
        return await response.json();
    },

    // GET /api/Suggestion/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/Suggestion/${id}`);
        if (!response.ok) throw new Error('Failed to fetch suggestion');
        return await response.json();
    },

    // GET /api/Suggestion/user/{id}
    getByUserId: async (userId) => {
        const response = await fetch(`${API_BASE_URL}/api/Suggestion/user/${userId}`);
        if (!response.ok) throw new Error('Failed to fetch suggestions by user');
        return await response.json();
    },

    // POST /api/Suggestion
    create: async (data, token) => {
        const headers = {
            'Content-Type': 'application/json',
        };
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }
        const response = await fetch(`${API_BASE_URL}/api/Suggestion/`, {
            method: 'POST',
            headers,
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to create suggestion');
        return await response.json();
    },

    // PUT /api/Suggestion
    update: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/Suggestion/`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to update suggestion');
        return await response.json();
    },

    // DELETE /api/Suggestion/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/Suggestion/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete suggestion');
        return true;
    }
};
