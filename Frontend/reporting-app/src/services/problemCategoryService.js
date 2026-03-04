const API_BASE_URL = 'http://localhost:80';

export const ProblemCategoryService = {
    // GET /api/ProblemCategory
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemCategory/`);
        if (!response.ok) throw new Error('Failed to fetch problem categories');
        return await response.json();
    },

    // GET /api/ProblemCategory/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemCategory/${id}`);
        if (!response.ok) throw new Error('Failed to fetch problem category');
        return await response.json();
    },

    // POST /api/ProblemCategory
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemCategory/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to create problem category');
        return await response.json();
    },

    // PUT /api/ProblemCategory
    update: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemCategory/`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to update problem category');
        return await response.json();
    },

    // DELETE /api/ProblemCategory/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemCategory/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete problem category');
        return true;
    }
};
