using McpGateway.Context;
using Microsoft.EntityFrameworkCore;

namespace McpGateway.Audit;

/// <summary>
/// EF Core-backed <see cref="IAuditSink"/> over McpAuditDB (Faza C). Registered as a SINGLETON (the
/// gatekeeper that consumes it is a singleton), so it creates a fresh context per operation via
/// <see cref="IDbContextFactory{TContext}"/> — never holding a scoped DbContext captive.
///
/// <para><b>RecordAsync is fail-closed</b> (throws on persistence failure) so the gatekeeper denies a
/// call whose decision could not be durably recorded. <b>UpdateOutcomeAsync</b> runs after execution
/// and is safe to call best-effort: a missing id is a no-op.</para>
/// </summary>
public class SqlAuditSink : IAuditSink
{
    private readonly IDbContextFactory<AuditDbContext> _factory;

    public SqlAuditSink(IDbContextFactory<AuditDbContext> factory) => _factory = factory;

    public async Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        // Fail-closed: SaveChangesAsync throws on a persistence failure and is NOT caught here — the
        // gatekeeper turns that into a Deny, so no action runs without a durable audit record.
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        db.AuditEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateOutcomeAsync(
        Guid id, AuditOutcome outcome, int durationMs, string? error,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var entry = await db.AuditEntries.FindAsync([id], cancellationToken);
        if (entry is null) return; // best-effort: a missing row is a no-op (never throws)

        entry.Outcome = outcome;
        entry.DurationMs = durationMs;
        entry.Error = error;
        await db.SaveChangesAsync(cancellationToken);
    }
}
