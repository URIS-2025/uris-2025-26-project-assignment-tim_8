using McpGateway.Authorization;

namespace McpGateway.Configuration;

/// <summary>Root of the "Gateway" appsettings section: the tool catalog + registered agents.</summary>
public class GatewayConfig
{
    public List<ToolPolicyConfig> Tools { get; set; } = new();
    public List<AgentConfig> Agents { get; set; } = new();
}

/// <summary>Config-bindable shape of a <see cref="ToolPolicy"/> (records don't bind cleanly).</summary>
public class ToolPolicyConfig
{
    public string ToolName { get; set; } = "";
    public string[] AllowedRoles { get; set; } = [];
    public bool OrgScoped { get; set; }
    public bool IsWrite { get; set; }

    public ToolPolicy ToDomain() => new(ToolName, AllowedRoles, OrgScoped, IsWrite);
}

/// <summary>Config-bindable shape of an <see cref="AgentPolicy"/>.</summary>
public class AgentConfig
{
    public string AgentId { get; set; } = "";
    public string PublicKeyPem { get; set; } = "";
    public string[] AllowedTools { get; set; } = [];

    public AgentPolicy ToDomain() => new(AgentId, PublicKeyPem, AllowedTools);
}
