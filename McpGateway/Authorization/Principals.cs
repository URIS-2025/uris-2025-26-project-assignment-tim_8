namespace McpGateway.Authorization;

/// <summary>
/// The end user on whose behalf a tool call is made — resolved from the forwarded
/// "on-behalf-of" OrganizationService JWT. <see cref="OrganizationId"/> is null for admins
/// (who are global / not org-scoped).
/// </summary>
public record UserContext(string UserId, string Role, Guid? OrganizationId);

/// <summary>
/// The AI agent making the call — resolved from the validated agent-JWT. Each agent carries
/// its own set of tools it is permitted to invoke (the "agent-identity" half of the
/// dual-principal model).
/// </summary>
public record AgentContext(string AgentId, IReadOnlySet<string> AllowedTools);
