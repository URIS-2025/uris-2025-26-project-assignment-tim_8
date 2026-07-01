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
}
