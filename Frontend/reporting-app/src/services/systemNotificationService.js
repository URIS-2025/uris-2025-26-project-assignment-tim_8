const API_BASE_URL = 'http://localhost:80';

export const SystemNotificationService = {
    // GET /api/SystemNotification
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/SystemNotification/`);
        if (!response.ok) throw new Error('Failed to fetch system notifications');
        return await response.json();
    },

    // POST /api/SystemNotification
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/SystemNotification/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to create system notification');
        return await response.json();
    },

    // DELETE /api/SystemNotification/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/SystemNotification/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete system notification');
        return true;
    }
};
