
const API_BASE_URL = 'http://localhost:80';

export const SuggestionBoxService = {

    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/SuggestionBox/`);
        if (!response.ok) throw new Error('Failed to fetch suggestion boxes');
        return await response.json();
    },
    // GET /api/SuggestionBox/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/SuggestionBox/${id}`);
        if (!response.ok) throw new Error('Failed to fetch suggestion box');
        return await response.json();
    },

    // GET /api/SuggestionBox/organization/{organizationId}
    getByOrganizationId: async (organizationId) => {
        const response = await fetch(
            `${API_BASE_URL}/api/SuggestionBox/organization/${organizationId}`
        );
        if (!response.ok) throw new Error('Failed to fetch suggestion boxes');
        return await response.json();
    },

    // POST /api/SuggestionBox
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/SuggestionBox/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });

        if (!response.ok) throw new Error('Failed to create suggestion box');
        return await response.json();
    },

    // PUT /api/SuggestionBox
    update: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/SuggestionBox/`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });

        if (!response.ok) throw new Error('Failed to update suggestion box');
        return await response.json();
    },

    // DELETE /api/SuggestionBox/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/SuggestionBox/${id}`, {
            method: 'DELETE',
        });

        if (!response.ok) throw new Error('Failed to delete suggestion box');
        return true;
    },

    // DELETE /api/SuggestionBox/organization/{organizationId}
    deleteByOrganizationId: async (organizationId) => {
        const response = await fetch(
            `${API_BASE_URL}/api/SuggestionBox/organization/${organizationId}`,
            {
                method: 'DELETE',
            }
        );

        if (!response.ok)
            throw new Error('Failed to delete suggestion boxes by organization');

        return true;
    }
};