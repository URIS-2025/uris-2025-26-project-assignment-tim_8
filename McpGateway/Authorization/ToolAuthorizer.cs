namespace McpGateway.Authorization;

/// <summary>
/// Evaluates the per-tool authorization policy on every tool call, combining the two principals
/// (agent + user) with the tool's <see cref="ToolPolicy"/>. Pure logic — no HTTP/framework
/// dependencies — so it is directly unit-testable. This is the heart of the gateway's
/// "per-tool authorization" mechanism.
/// </summary>
public class ToolAuthorizer
{
    private const string AdminRole = "Admin";

    private readonly IReadOnlyDictionary<string, ToolPolicy> _policies;

    public ToolAuthorizer(IReadOnlyDictionary<string, ToolPolicy> policies)
    {
        _policies = policies;
    }

    /// <summary>
    /// Decides whether <paramref name="agent"/> acting for <paramref name="user"/> may invoke
    /// <paramref name="toolName"/>. Returns an <see cref="AuthResult"/> with the decision, a
    /// human-readable reason (recorded in the audit trail), and the effective org scope.
    /// </summary>
    public AuthResult Authorize(string toolName, AgentContext agent, UserContext user)
    {
        // 1. The tool must be a known, registered tool.
        if (!_policies.TryGetValue(toolName, out var policy))
            return AuthResult.Deny($"nepoznat alat '{toolName}'");

        // 2. Agent dimension: this agent must be permitted to invoke this tool.
        if (!agent.AllowedTools.Contains(toolName))
            return AuthResult.Deny($"agent '{agent.AgentId}' nema pravo na alat '{toolName}'");

        // 3. User dimension: the user's role must be allowed by the tool's policy. Role titles are
        //    free-form in OrganizationService (UserRole.Title), so match case-insensitively.
        if (!policy.AllowedRoles.Contains(user.Role, StringComparer.OrdinalIgnoreCase))
            return AuthResult.Deny($"rola '{user.Role}' nema pravo na alat '{toolName}'");

        // 4. Org-scope: for an org-scoped tool a non-admin caller is LOCKED to their own org —
        //    the gateway forces it here so the LLM can never widen scope. Admins are global.
        if (policy.OrgScoped && !string.Equals(user.Role, AdminRole, StringComparison.OrdinalIgnoreCase))
        {
            if (user.OrganizationId is null)
                return AuthResult.Deny("korisnik nema organizaciju u tokenu");

            return AuthResult.Allow(user.OrganizationId);
        }

        // Admin, or a tool that is not org-scoped: no org restriction.
        return AuthResult.Allow(null);
    }
}
