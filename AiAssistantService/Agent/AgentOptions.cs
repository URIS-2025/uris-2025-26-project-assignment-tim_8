namespace AiAssistantService.Agent;

/// <summary>Bound from the "Agent" configuration section. Loop knobs, the write allow-list, the
/// system prompt, and the agent-credential settings. <see cref="PrivateKeyPem"/> is a SECRET —
/// supplied via environment / user-secrets, never committed.</summary>
public class AgentOptions
{
    public const string SectionName = "Agent";

    // ── LLM loop ──────────────────────────────────────────────────────────────
    public string Model { get; set; } = "claude-sonnet-5";
    public int MaxTokens { get; set; } = 4096;
    public int MaxIterations { get; set; } = 8;
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Allow-list of tools safe to auto-execute (the read tools). The propose-confirm gate is
    /// FAIL-SAFE: only tools on this list run without confirmation — every other tool (writes AND
    /// any tool not listed here, e.g. one added to the gateway later) is treated as requiring human
    /// confirmation. An empty list means "confirm everything".
    /// </summary>
    public string[] ReadTools { get; set; } = Array.Empty<string>();

    // ── Agent credential (agent-JWT, RS256) ───────────────────────────────────
    public string AgentId { get; set; } = "ai-assistant";
    public string Audience { get; set; } = "mcp-gateway";
    public int TokenLifetimeSeconds { get; set; } = 120;

    /// <summary>PEM-encoded RSA PRIVATE key used to sign the agent-JWT. SECRET.</summary>
    public string PrivateKeyPem { get; set; } = string.Empty;
}
