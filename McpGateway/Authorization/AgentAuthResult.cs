namespace McpGateway.Authorization;

/// <summary>
/// Outcome of validating an agent-JWT. On success it carries the resolved
/// <see cref="AgentContext"/> (agent id + allowed tools); on failure it carries a reason that is
/// recorded in the audit trail.
/// </summary>
public record AgentAuthResult(AgentContext? Agent, string? Error)
{
    public bool Succeeded => Agent is not null;

    public static AgentAuthResult Success(AgentContext agent) => new(agent, null);
    public static AgentAuthResult Fail(string error) => new(null, error);
}
