const API_BASE_URL = 'http://localhost:80';

export const ProblemCommentService = {
    // GET /api/ProblemComment
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemComment/`);
        if (!response.ok) throw new Error('Failed to fetch problem comments');
        return await response.json();
    },

    // GET /api/ProblemComment/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemComment/${id}`);
        if (!response.ok) throw new Error('Failed to fetch problem comment');
        return await response.json();
    },

    // GET /api/ProblemComment/problem/{id}
    getByProblemId: async (problemId) => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemComment/problem/${problemId}`);
        if (!response.ok) throw new Error('Failed to fetch comments by problem');
        return await response.json();
    },

    // POST /api/ProblemComment
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemComment/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to create problem comment');
        return await response.json();
    },

    // PUT /api/ProblemComment
    update: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemComment/`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to update problem comment');
        return await response.json();
    },

    // DELETE /api/ProblemComment/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/ProblemComment/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete problem comment');
        return true;
    }
};
