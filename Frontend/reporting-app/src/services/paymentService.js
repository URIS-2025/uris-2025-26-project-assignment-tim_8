const API_BASE_URL = 'http://127.0.0.1:80';

export const PaymentService = {
    // GET /api/Payment
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/Payment/`);
        if (!response.ok) throw new Error('Failed to fetch payments');
        return await response.json();
    },

    // GET /api/Payment/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/Payment/${id}`);
        if (!response.ok) throw new Error('Failed to fetch payment');
        return await response.json();
    },

    // GET /api/Payment/bySubscription/{subscriptionId}
    getBySubscriptionId: async (subscriptionId) => {
        const response = await fetch(`${API_BASE_URL}/api/Payment/bySubscription/${subscriptionId}`);
        if (!response.ok) throw new Error('Failed to fetch payments by subscription');
        return await response.json();
    },

    // POST /api/Payment
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/Payment/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to create payment');
        return await response.json();
    },

    // PUT /api/Payment
    update: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/Payment/`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to update payment');
        return await response.json();
    },

    // DELETE /api/Payment/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/Payment/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete payment');
        return true;
    }
};
