const API_BASE_URL = 'http://127.0.0.1:80';

const getAuthHeader = () => {
    const token = localStorage.getItem('authToken');
    return token ? { Authorization: `Bearer ${token}` } : {};
};

const extractErrorMessage = async (response) => {
    try {
        const data = await response.json();
        if (data.errors) {
            return Object.values(data.errors).flat().join(' ');
        }
        return data.error || data.title || 'Request failed';
    } catch {
        return 'Request failed';
    }
};

export const UserService = {
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/User/`, {
            headers: { ...getAuthHeader() },
        });
        if (!response.ok) throw new Error(await extractErrorMessage(response));
        return response.json();
    },

    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/User/${id}`, {
            headers: { ...getAuthHeader() },
        });
        if (!response.ok) throw new Error(await extractErrorMessage(response));
        return response.json();
    },

    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/User/`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error(await extractErrorMessage(response));
        return response.json();
    },

    update: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/User/`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json', ...getAuthHeader() },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error(await extractErrorMessage(response));
        return response.json();
    },

    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/User/${id}`, {
            method: 'DELETE',
            headers: { ...getAuthHeader() },
        });
        if (!response.ok) throw new Error(await extractErrorMessage(response));
        return true;
    },

    // Returns { accessToken, refreshToken }
    login: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/User/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error(await extractErrorMessage(response));
        return response.json();
    },

    refresh: async (refreshToken) => {
        const response = await fetch(`${API_BASE_URL}/api/User/refresh`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ refreshToken }),
        });
        if (!response.ok) throw new Error('Session expired. Please log in again.');
        return response.json();
    },

    invite: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/User/invite/`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', ...getAuthHeader() },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error(await extractErrorMessage(response));
        return response.json();
    },

    updateRole: async (id, roleData) => {
        const response = await fetch(`${API_BASE_URL}/api/User/${id}/role/`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json', ...getAuthHeader() },
            body: JSON.stringify(roleData),
        });
        if (!response.ok) throw new Error(await extractErrorMessage(response));
        return response.json();
    },
};
