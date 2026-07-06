using System.Collections.Concurrent;

namespace McpGateway.Audit;

/// <summary>
/// In-memory audit sink for Faza A (and for tests). Thread-safe; keyed by <see cref="AuditEntry.Id"/>
/// so a decision record can be enriched in place with its execution outcome. Replaced by the
/// AuditDB-backed <c>SqlAuditSink</c> in production (Faza C).
/// </summary>
public class InMemoryAuditSink : IAuditSink
{
    private readonly ConcurrentDictionary<Guid, AuditEntry> _entries = new();

    public Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        _entries[entry.Id] = entry;
        return Task.CompletedTask;
    }

    public Task UpdateOutcomeAsync(
        Guid id, AuditOutcome outcome, int durationMs, string? error,
        CancellationToken cancellationToken = default)
    {
        // Best-effort: enrich in place if present; a missing id is a no-op (never throws).
        _entries.TryGetValue(id, out var existing);
        if (existing is not null)
            _entries[id] = existing with { Outcome = outcome, DurationMs = durationMs, Error = error };
        return Task.CompletedTask;
    }

    public IReadOnlyList<AuditEntry> Entries => _entries.Values.ToArray();
}
