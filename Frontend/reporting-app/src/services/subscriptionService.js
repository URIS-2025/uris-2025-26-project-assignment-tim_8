const API_BASE_URL = 'http://localhost:80';

export const SubscriptionService = {
    // GET /api/Subscription
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/Subscription/`);
        if (!response.ok) throw new Error('Failed to fetch subscriptions');
        return await response.json();
    },

    // GET /api/Subscription/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/Subscription/${id}`);
        if (!response.ok) throw new Error('Failed to fetch subscription');
        return await response.json();
    },

    // POST /api/Subscription
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/Subscription/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to create subscription');
        return await response.json();
    },

    // PUT /api/Subscription
    update: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/Subscription/`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to update subscription');
        return await response.json();
    },

    // DELETE /api/Subscription/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/Subscription/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete subscription');
        return true;
    }
};
