namespace McpGateway.Authorization;

/// <summary>
/// A registered agent, from configuration. The gateway holds only the agent's <b>public</b> key
/// (PEM/SPKI) — it can verify the agent's signed token but can never forge one. Each agent also
/// declares the set of tools it is allowed to invoke (the "agent-identity" allow-list).
/// </summary>
public record AgentPolicy(string AgentId, string PublicKeyPem, string[] AllowedTools);
