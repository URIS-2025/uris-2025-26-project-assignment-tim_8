const API_BASE_URL = 'http://localhost:80';

export const BoxAccessLinkService = {
    // GET /api/BoxAccessLink
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/BoxAccessLink/`);
        if (!response.ok) throw new Error('Failed to fetch box access links');
        return await response.json();
    },

    // GET /api/BoxAccessLink/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/BoxAccessLink/${id}`);
        if (!response.ok) throw new Error('Failed to fetch box access link');
        return await response.json();
    }
};
