// Builds the GET /api/Audit query string from the dashboard's filter state.
//
// Two rules encode security/contract facts:
//   1. A non-admin caller NEVER sends `organizationId` — the gateway force-scopes a manager to their
//      own org and ignores a client-supplied value, so sending one is pointless and misleading.
//   2. Only ''/null/undefined are dropped. The value 0 (decision=Allow, outcome=Success, page=0) is
//      meaningful and MUST survive — a naive falsy check would wrongly drop it.

const isEmpty = (v) => v === '' || v === null || v === undefined;

export const buildAuditQuery = (filters = {}, { isAdmin = false } = {}) => {
    const { page = 0, pageSize = 50, organizationId, ...rest } = filters;

    const params = new URLSearchParams();
    for (const [key, value] of Object.entries(rest)) {
        if (!isEmpty(value)) params.append(key, value);
    }
    // organizationId only for an admin (a manager is force-scoped server-side).
    if (isAdmin && !isEmpty(organizationId)) params.append('organizationId', organizationId);

    params.append('page', page);
    params.append('pageSize', pageSize);
    return params.toString();
};
