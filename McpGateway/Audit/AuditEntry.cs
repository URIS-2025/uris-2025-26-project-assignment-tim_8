using McpGateway.Authorization;

namespace McpGateway.Audit;

/// <summary>Result of executing a tool after it was authorized.</summary>
public enum AuditOutcome { Success, Error, NotExecuted }

/// <summary>For write tools (human-in-the-loop): lifecycle of the proposed action.</summary>
public enum ConfirmationState { Proposed, Confirmed, Rejected }

/// <summary>
/// One audit record — the durable evidence of a single tool call through the gateway. Captures
/// BOTH principals (agent + user), the tool, a scrubbed argument summary, the authorization
/// decision + reason, and the execution outcome. The tool's raw result is deliberately NOT stored
/// (it may contain org data). This is the schema the audit-review dashboard reads (Faza C/F).
/// </summary>
public record AuditEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Links the chain of tool calls that came from one user request (LLM chaining).</summary>
    public Guid? CorrelationId { get; init; }

    // Dual-principal — "activity of users AND agents".
    public required string AgentId { get; init; }
    public required string UserId { get; init; }
    public required string UserRole { get; init; }
    public Guid? OrganizationId { get; init; }

    public required string ToolName { get; init; }
    public bool IsWrite { get; init; }
    public string? ArgsSummary { get; init; }

    // The per-tool authorization decision.
    public AuthDecision Decision { get; init; }
    public required string DecisionReason { get; init; }

    // Write-tool confirmation lifecycle (null for reads).
    public ConfirmationState? Confirmation { get; init; }

    // Execution outcome (meaningful only when allowed + executed).
    public AuditOutcome Outcome { get; init; }
    public string? Error { get; init; }
    public int? DurationMs { get; init; }
}
