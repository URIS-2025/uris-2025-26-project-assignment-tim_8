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

export const AnonymousUserService = {
    getAll: async () => {
        const response = await fetch(`${API_BASE_URL}/api/AnonymousUser/`, {
            headers: { ...getAuthHeader() },
        });
        if (!response.ok) throw new Error(await extractErrorMessage(response));
        return response.json();
    },

    getById: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/AnonymousUser/${id}`, {
            headers: { ...getAuthHeader() },
        });
        if (!response.ok) throw new Error(await extractErrorMessage(response));
        return response.json();
    },

    // Returns created user DTO
    create: async (data) => {
        const response = await fetch(`${API_BASE_URL}/api/AnonymousUser/`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error(await extractErrorMessage(response));
        return response.json();
    },

    delete: async (id) => {
        const response = await fetch(`${API_BASE_URL}/api/AnonymousUser/${id}`, {
            method: 'DELETE',
            headers: { ...getAuthHeader() },
        });
        if (!response.ok) throw new Error(await extractErrorMessage(response));
        return true;
    },

    // Returns { accessToken, refreshToken }
    login: async (credentials) => {
        const response = await fetch(`${API_BASE_URL}/api/AnonymousUser/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(credentials),
        });
        if (!response.ok) throw new Error(await extractErrorMessage(response));
        return response.json();
    },

    refresh: async (refreshToken) => {
        const response = await fetch(`${API_BASE_URL}/api/AnonymousUser/refresh`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ refreshToken }),
        });
        if (!response.ok) throw new Error('Session expired. Please log in again.');
        return response.json();
    },
};
