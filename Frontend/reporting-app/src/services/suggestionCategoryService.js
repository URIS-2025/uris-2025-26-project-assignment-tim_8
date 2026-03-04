const API_BASE_URL = 'http://localhost:80';

export const SuggestionCategoryService = {
    // GET /api/SuggestionCategory
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/SuggestionCategory/`);
        if (!response.ok) throw new Error('Failed to fetch suggestion categories');
        return await response.json();
    },

    // GET /api/SuggestionCategory/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/SuggestionCategory/${id}`);
        if (!response.ok) throw new Error('Failed to fetch suggestion category');
        return await response.json();
    },

    // POST /api/SuggestionCategory
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/SuggestionCategory/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to create suggestion category');
        return await response.json();
    },

    // PUT /api/SuggestionCategory
    update: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/SuggestionCategory/`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to update suggestion category');
        return await response.json();
    },

    // DELETE /api/SuggestionCategory/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/SuggestionCategory/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete suggestion category');
        return true;
    }
};
