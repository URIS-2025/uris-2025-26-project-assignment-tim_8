const API_BASE_URL = 'http://localhost:80';

export const ProblemService = {
    // GET /api/Problem
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/Problem/`);
        if (!response.ok) throw new Error('Failed to fetch problems');
        return await response.json();
    },

    // GET /api/Problem/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/Problem/${id}`);
        if (!response.ok) throw new Error('Failed to fetch problem');
        return await response.json();
    },

    // GET /api/Problem/problembox/{problemBoxId}
    getByProblemBoxId: async (problemBoxId) => {
        const response = await fetch(`${API_BASE_URL}/api/Problem/problembox/${problemBoxId}`);
        if (!response.ok) throw new Error('Failed to fetch problems by problem box');
        return await response.json();
    },

    // POST /api/Problem
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/Problem/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to create problem');
        return await response.json();
    },

    // PUT /api/Problem
    update: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/Problem/`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to update problem');
        return await response.json();
    },

    // DELETE /api/Problem/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/Problem/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete problem');
        return true;
    }
};
