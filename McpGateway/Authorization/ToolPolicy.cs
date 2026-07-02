namespace McpGateway.Authorization;

/// <summary>
/// Authorization policy for a single MCP tool — the "per-tool" part of the gateway's
/// authorization model. It declares which user roles may invoke the tool, whether the tool
/// is scoped to the caller's organization, and whether it mutates state (write).
/// </summary>
public record ToolPolicy(
    string ToolName,
    string[] AllowedRoles,
    bool OrgScoped,
    bool IsWrite);
