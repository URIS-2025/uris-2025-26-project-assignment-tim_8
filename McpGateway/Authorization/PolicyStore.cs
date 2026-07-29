namespace McpGateway.Authorization;

/// <summary>
/// Loaded, validated policy configuration for the gateway: the per-tool policies and the
/// registered agents. Validates on construction (fail-fast at startup) that every tool an agent
/// is allowed to call is a real, known tool, AND that no policy can express a cross-org fail-open.
/// Feeds <see cref="ToolAuthorizer"/> and <see cref="AgentTokenValidator"/>.
/// </summary>
/// <remarks>
/// The org-scope guarantee is otherwise expressible only in <c>appsettings.json</c> and enforced
/// nowhere: <see cref="ToolAuthorizer"/> populates <c>EffectiveOrganizationId</c> only for
/// <c>OrgScoped</c> tools, <see cref="ToolScope"/> reads a null id as "admin — global", and
/// <c>WriteTools.BoxInScopeAsync</c> then allows any box. A single token
/// (<c>"OrgScoped": false</c> on a tool a Manager can call) therefore turns the forced-org guard
/// into an LLM-supplied parameter, with no code change and no failing test. Because the null
/// sentinel means both "admin/global" and "not org-scoped", the write path cannot recover the
/// distinction at runtime — so the invariant is asserted here, at boot, where it still can be.
/// </remarks>
public class PolicyStore
{
    public IReadOnlyDictionary<string, ToolPolicy> ToolPolicies { get; }
    public IReadOnlyList<AgentPolicy> Agents { get; }

    public PolicyStore(IEnumerable<ToolPolicy> toolPolicies, IEnumerable<AgentPolicy> agents)
    {
        var tools = toolPolicies.ToDictionary(t => t.ToolName);
        var agentList = agents.ToList();

        // Fail-fast: an agent must never be granted a tool that isn't in the catalog.
        foreach (var agent in agentList)
            foreach (var tool in agent.AllowedTools)
                if (!tools.ContainsKey(tool))
                    throw new InvalidOperationException(
                        $"Agent '{agent.AgentId}' referencira nepoznat alat '{tool}'.");

        // Fail-fast: a write must always be org-scoped, and a non-org-scoped tool may only ever be
        // granted to Admin. Either violation is a cross-org fail-open (see <remarks>).
        foreach (var tool in tools.Values)
        {
            if (tool.IsWrite && !tool.OrgScoped)
                throw new InvalidOperationException(
                    $"Alat '{tool.ToolName}' je write ali nije OrgScoped — to bi dozvolilo " +
                    "upis u tuđu organizaciju. Svaki write mora biti OrgScoped.");

            if (!tool.OrgScoped && tool.AllowedRoles.Any(
                    r => !string.Equals(r, ToolAuthorizer.AdminRole, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException(
                    $"Alat '{tool.ToolName}' nije OrgScoped, pa sme biti dodeljen samo roli " +
                    $"'{ToolAuthorizer.AdminRole}' — inače se org-scope preuzima iz argumenata alata.");
        }

        ToolPolicies = tools;
        Agents = agentList;
    }
}
