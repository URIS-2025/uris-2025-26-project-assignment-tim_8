const API_BASE_URL = 'http://127.0.0.1:80';

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

    // GET /api/ProblemComment/problem/{id} -> Fallback to getAll + filter if specific route is 404
    getByProblemId: async (problemId) => {
        try {
            const response = await fetch(`${API_BASE_URL}/api/ProblemComment/problem/${problemId}`);
            if (response.ok) return await response.json();

            // If 404 or other error, fallback to getAll and filter
            console.warn(`Specific route /api/ProblemComment/problem/${problemId} failed, falling back to getAll()`);
            const allComments = await ProblemCommentService.getAll();
            return allComments.filter(c => (c.problemId === problemId || c.ProblemId === problemId));
        } catch (err) {
            console.error('Error in getByProblemId, trying fallback:', err);
            try {
                const allComments = await ProblemCommentService.getAll();
                return allComments.filter(c => (c.problemId === problemId || c.ProblemId === problemId));
            } catch (fallbackErr) {
                console.error('Final fallback failed:', fallbackErr);
                return [];
            }
        }
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
