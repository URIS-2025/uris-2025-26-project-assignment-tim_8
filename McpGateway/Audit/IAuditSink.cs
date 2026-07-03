namespace McpGateway.Audit;

/// <summary>
/// Sink for durable audit records. This is a first-class, reliable write (NOT best-effort
/// fire-and-forget logging) — the gateway awaits it so every decision, allow or deny, is recorded.
/// Faza A ships an in-memory implementation; Faza C swaps in an EF-backed AuditDB sink.
/// <para>
/// <b>Contract (fail-closed):</b> an implementation MUST THROW if it cannot durably persist the
/// entry. The gateway treats a failed audit write as a hard failure and denies the call, so that
/// no action is ever executed without a corresponding audit record. Implementations must NOT
/// swallow persistence errors (no <c>try/catch { }</c>) — a silent drop would lose exactly the
/// records that matter most during an incident.
/// </para>
/// </summary>
public interface IAuditSink
{
    Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enriches an already-recorded entry (by <paramref name="id"/>) with the execution outcome once
    /// the tool has run. Called AFTER execution, so — unlike <see cref="RecordAsync"/> — it is
    /// best-effort at the call site: the action already happened and cannot be undone, so a failure
    /// here must not fail the request. A missing id is a no-op (never throws for "not found").
    /// </summary>
    Task UpdateOutcomeAsync(
        Guid id, AuditOutcome outcome, int durationMs, string? error,
        CancellationToken cancellationToken = default);
}
