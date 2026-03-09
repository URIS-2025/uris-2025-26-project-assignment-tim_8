const API_BASE_URL = 'http://127.0.0.1:80';

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

    // POST /api/AnonymousUser
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/AnonymousUser/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to create anonymous user');
        return await response.json();
    },

    // DELETE /api/AnonymousUser/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/AnonymousUser/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete anonymous user');
        return true;
    },

    // POST /api/AnonymousUser/login
    login: async (credentials) => {
        const response = await fetch(`${API_BASE_URL}/api/AnonymousUser/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(credentials),
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.error || 'Invalid username or password');
        }

        // The endpoint returns a token string
        return await response.text();
    }
};
