const API_BASE_URL = 'http://127.0.0.1:80';

export const BillingNotificationService = {
    // GET /api/BillingNotification
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/BillingNotification/`);
        if (!response.ok) throw new Error('Failed to fetch billing notifications');
        return await response.json();
    },

    // GET /api/BillingNotification/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/BillingNotification/${id}`);
        if (!response.ok) throw new Error('Failed to fetch billing notification');
        return await response.json();
    },

    // POST /api/BillingNotification
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/BillingNotification/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to create billing notification');
        return await response.json();
    },

    // PUT /api/BillingNotification
    update: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/BillingNotification/`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to update billing notification');
        return await response.json();
    },

    // DELETE /api/BillingNotification/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/BillingNotification/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete billing notification');
        return true;
    }
};
