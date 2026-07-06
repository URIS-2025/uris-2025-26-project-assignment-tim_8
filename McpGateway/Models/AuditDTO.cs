using McpGateway.Audit;
using McpGateway.Authorization;

namespace McpGateway.Models;

/// <summary>
/// Scrubbed projection of an <see cref="AuditEntry"/> returned by <c>GET /api/Audit</c> — the entity
/// is never returned directly (repo convention). By construction it carries no raw error bodies or
/// PII: <see cref="Error"/> holds the same sanitized message the tool returned to the agent, and
/// <see cref="ArgsSummary"/> is already the deny-by-default scrubbed summary.
/// </summary>
public record AuditDTO(
    Guid Id,
    DateTime Timestamp,
    string AgentId,
    string UserId,
    string UserRole,
    Guid? OrganizationId,
    string ToolName,
    bool IsWrite,
    string? ArgsSummary,
    AuthDecision Decision,
    string DecisionReason,
    ConfirmationState? Confirmation,
    AuditOutcome Outcome,
    string? Error,
    int? DurationMs);

/// <summary>A page of audit records plus the total count matching the filter (for pagination UI).</summary>
public record AuditPageDTO(int Total, IReadOnlyList<AuditDTO> Items);
