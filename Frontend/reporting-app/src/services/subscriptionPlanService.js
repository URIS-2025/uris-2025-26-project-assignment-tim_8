const API_BASE_URL = 'http://localhost:80';

export const SubscriptionPlanService = {
    // GET /api/SubscriptionPlan
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/SubscriptionPlan/`);
        if (!response.ok) throw new Error('Failed to fetch subscription plans');
        return await response.json();
    },

    // GET /api/SubscriptionPlan/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/SubscriptionPlan/${id}`);
        if (!response.ok) throw new Error('Failed to fetch subscription plan');
        return await response.json();
    }
};
