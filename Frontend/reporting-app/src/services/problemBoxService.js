const API_BASE_URL = 'http://127.0.0.1:80';

export const ProblemBoxService = {
    // GET /api/ProblemBox
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemBox/`);
        if (!response.ok) throw new Error('Failed to fetch problem boxes');
        return await response.json();
    },

    // GET /api/ProblemBox/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemBox/${id}`);
        if (!response.ok) throw new Error('Failed to fetch problem box');
        return await response.json();
    },

    // GET /api/ProblemBox/organization/{id}
    getByOrganizationId: async (organizationId) => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemBox/organization/${organizationId}`);
        if (!response.ok) throw new Error('Failed to fetch problem boxes by organization');
        return await response.json();
    },

    // GET /api/ProblemBox/boxaccesslink/{boxAccessLinkId}
    getByAccessLinkId: async (boxAccessLinkId) => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemBox/boxaccesslink/${boxAccessLinkId}`);
        if (!response.ok) throw new Error('Failed to fetch problem box by access link');
        return await response.json();
    },

    // POST /api/ProblemBox
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemBox/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to create problem box');
        return await response.json();
    },

    // PUT /api/ProblemBox
    update: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemBox/`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to update problem box');
        return await response.json();
    },

    // PUT /api/ProblemBox/{id}/status
    setStatus: async (id, status) => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemBox/${id}/status`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ status }),
        });
        if (!response.ok) throw new Error('Failed to update problem box status');
        return await response.json();
    },

    // DELETE /api/ProblemBox/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemBox/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete problem box');
        return true;
    }
};
