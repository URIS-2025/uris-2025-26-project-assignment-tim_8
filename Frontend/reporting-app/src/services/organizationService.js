const API_BASE_URL = 'https://localhost:7290';

export const OrganizationService = {
    // GET /api/Organization
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/Organization`);
        if (!response.ok) throw new Error('Failed to fetch organizations');
        return await response.json();
    },

    // GET /api/Organization/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/Organization/${id}`);
        if (!response.ok) throw new Error('Failed to fetch organization details');
        return await response.json();
    },

    // POST /api/Organization
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/Organization`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to create organization');
        return await response.json();
    },

    // PUT /api/Organization
    update: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/Organization`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to update organization');
        return await response.json();
    },

    // DELETE /api/Organization/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/Organization/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete organization');
        return true;
    }
};
