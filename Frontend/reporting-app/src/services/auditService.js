import { buildAuditQuery } from '../utils/auditQuery';

const API_BASE_URL = 'http://127.0.0.1:80';

// GET /api/Audit is [Authorize(Roles="Admin,Manager")] — the bearer is required or the gateway 401s.
const getAuthHeader = () => {
    const token = localStorage.getItem('authToken');
    return token ? { Authorization: `Bearer ${token}` } : {};
};

const extractErrorMessage = async (response) => {
    try {
        const data = await response.json();
        return data.error || data.message || data.title || 'Failed to load audit log.';
    } catch {
        return 'Failed to load audit log.';
    }
};

export const AuditService = {
    // filters: { toolName, decision, outcome, agentId, userId, organizationId, from, to, page, pageSize }
    // opts.isAdmin gates whether organizationId is sent (a manager is force-scoped server-side).
    getAudit: async (filters = {}, opts = {}) => {
        const qs = buildAuditQuery(filters, opts);
        const response = await fetch(`${API_BASE_URL}/api/Audit/?${qs}`, {
            headers: { ...getAuthHeader() },
        });
        if (!response.ok) throw new Error(await extractErrorMessage(response));
        return response.json();
    },
};
