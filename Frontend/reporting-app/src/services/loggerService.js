const API_BASE_URL = 'http://localhost:80';

export const LoggerService = {
    // GET /api/Logger?take=100
    getAll: async (take = 100) => {
        const response = await fetch(`${API_BASE_URL}/api/Logger/?take=${take}`);
        if (!response.ok) throw new Error('Failed to fetch logs');
        return await response.json();
    },

    // GET /api/Logger/{id}
    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/Logger/${id}`);
        if (!response.ok) throw new Error('Failed to fetch log');
        return await response.json();
    },

    // GET /api/Logger/search?userId=...&action=...&entityName=...&serviceName=...&httpMethod=...&isSuccess=...&fromUtc=...&toUtc=...&take=100
    search: async (params = {}) => {
        const queryParams = new URLSearchParams();
        if (params.userId) queryParams.append('userId', params.userId);
        if (params.action) queryParams.append('action', params.action);
        if (params.entityName) queryParams.append('entityName', params.entityName);
        if (params.serviceName) queryParams.append('serviceName', params.serviceName);
        if (params.httpMethod) queryParams.append('httpMethod', params.httpMethod);
        if (params.isSuccess !== undefined) queryParams.append('isSuccess', params.isSuccess);
        if (params.fromUtc) queryParams.append('fromUtc', params.fromUtc);
        if (params.toUtc) queryParams.append('toUtc', params.toUtc);
        if (params.take) queryParams.append('take', params.take);

        const response = await fetch(`${API_BASE_URL}/api/Logger/search?${queryParams.toString()}`);
        if (!response.ok) throw new Error('Failed to search logs');
        return await response.json();
    },

    // POST /api/Logger
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/Logger/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Failed to create log');
        return await response.json();
    },

    // DELETE /api/Logger/{id}
    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/Logger/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) throw new Error('Failed to delete log');
        return true;
    }
};
