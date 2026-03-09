const API_BASE_URL = 'https://127.0.0.1:80';

export const SystemUserService = {
    // GET /api/SystemUser
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/SystemUser`);
        if (!response.ok) throw new Error('Failed to fetch users');
        return await response.json();
    },

    // GET /api/SystemUser/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/SystemUser/${id}`);
        if (!response.ok) throw new Error('Failed to fetch user details');
        return await response.json();
    },

    // POST /api/SystemUser/invite
    invite: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/SystemUser/invite`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to invite user');
        return await response.json();
    },

    // PUT /api/SystemUser/{id}/role
    updateRole: async (id, roleData) => {
        const response = await fetch(`${API_BASE_URL}/api/SystemUser/${id}/role`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(roleData),
        });
        if (!response.ok) throw new Error('Failed to update user role');
        return await response.json();
    },

    // DELETE /api/SystemUser/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/SystemUser/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete user');
        return true;
    }
};
