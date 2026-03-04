const API_BASE_URL = 'http://localhost:80';

export const AttachmentService = {
    // GET /api/Attachment
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/Attachment/`);
        if (!response.ok) throw new Error('Failed to fetch attachments');
        return await response.json();
    },

    // GET /api/Attachment/suggestion/{id}
    getBySuggestionId: async (suggestionId) => {
        const response = await fetch(`${API_BASE_URL}/api/Attachment/suggestion/${suggestionId}`);
        if (!response.ok) throw new Error('Failed to fetch attachments by suggestion');
        return await response.json();
    },

    // GET /api/Attachment/problem/{id}
    getByProblemId: async (problemId) => {
        const response = await fetch(`${API_BASE_URL}/api/Attachment/problem/${problemId}`);
        if (!response.ok) throw new Error('Failed to fetch attachments by problem');
        return await response.json();
    },

    // POST /api/Attachment
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/Attachment/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to create attachment');
        return await response.json();
    },

    // PUT /api/Attachment
    update: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/Attachment/`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to update attachment');
        return await response.json();
    },

    // DELETE /api/Attachment/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/Attachment/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete attachment');
        return true;
    }
};
