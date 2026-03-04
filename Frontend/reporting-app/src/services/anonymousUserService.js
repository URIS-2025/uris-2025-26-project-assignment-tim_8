const API_BASE_URL = 'http://localhost:80';

export const AnonymousUserService = {
    // GET /api/AnonymousUser
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/AnonymousUser/`);
        if (!response.ok) throw new Error('Failed to fetch anonymous users');
        return await response.json();
    },

    // GET /api/AnonymousUser/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/AnonymousUser/${id}`);
        if (!response.ok) throw new Error('Failed to fetch anonymous user');
        return await response.json();
    },

    // DELETE /api/AnonymousUser/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/AnonymousUser/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete anonymous user');
        return true;
    }
};
