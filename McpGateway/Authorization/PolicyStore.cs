namespace McpGateway.Authorization;

/// <summary>
/// Loaded, validated policy configuration for the gateway: the per-tool policies and the
/// registered agents. Validates on construction (fail-fast at startup) that every tool an agent
/// is allowed to call is a real, known tool. Feeds <see cref="ToolAuthorizer"/> and
/// <see cref="AgentTokenValidator"/>.
/// </summary>
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

        ToolPolicies = tools;
        Agents = agentList;
    }
}
