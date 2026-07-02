using System.Collections.Concurrent;

namespace McpGateway.Audit;

/// <summary>
/// In-memory audit sink for Faza A (and for tests). Thread-safe append; exposes a snapshot of
/// recorded entries. Replaced by an AuditDB-backed sink in Faza C.
/// </summary>
public class InMemoryAuditSink : IAuditSink
{
    private readonly ConcurrentQueue<AuditEntry> _entries = new();

    public Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        _entries.Enqueue(entry);
        return Task.CompletedTask;
    }

    public IReadOnlyList<AuditEntry> Entries => _entries.ToArray();
}
