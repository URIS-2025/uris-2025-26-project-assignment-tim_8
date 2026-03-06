const API_BASE_URL = 'http://localhost:80';

export const UserRoleService = {
    // GET /api/UserRole
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/UserRole/`);
        if (!response.ok) throw new Error('Failed to fetch user roles');
        return await response.json();
    },

    // GET /api/UserRole/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/UserRole/${id}/`);
        if (!response.ok) throw new Error('Failed to fetch user role');
        return await response.json();
    },

    // POST /api/UserRole
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/UserRole/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to create user role');
        return await response.json();
    },

    // PUT /api/UserRole
    update: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/UserRole/`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to update user role');
        return await response.json();
    },

    // DELETE /api/UserRole/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/UserRole/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete user role');
        return true;
    }
};
