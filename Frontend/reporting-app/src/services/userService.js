const API_BASE_URL = 'http://localhost:80';

export const UserService = {
    // GET /api/User
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/User/`);
        if (!response.ok) throw new Error('Failed to fetch users');
        return await response.json();
    },

    // GET /api/User/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/User/${id}`);
        if (!response.ok) throw new Error('Failed to fetch user');
        return await response.json();
    },

    // POST /api/User
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/User/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to create user');
        return await response.json();
    },

    // PUT /api/User
    update: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/User/`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to update user');
        return await response.json();
    },

    // DELETE /api/User/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/User/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete user');
        return true;
    },

    // POST /api/User/login
    login: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/User/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Login failed');
        return await response.json();
    }
};
