using McpGateway.Audit;
using McpGateway.Authorization;
using McpGateway.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpGatewayTests;

/// <summary>
/// <see cref="SqlAuditSink"/> over an EF InMemory-backed context factory (the real views/SQL can't run
/// in a unit test). Proves durable record + in-place outcome enrichment + best-effort no-op on a
/// missing id. The fail-closed throw of RecordAsync (no swallow) is covered at the gatekeeper level.
/// </summary>
public class SqlAuditSinkTests
{
    // Minimal IDbContextFactory over one shared InMemory database (same name = same store).
    private sealed class InMemoryFactory : IDbContextFactory<AuditDbContext>
    {
        private readonly DbContextOptions<AuditDbContext> _options;
        public InMemoryFactory(string dbName) =>
            _options = new DbContextOptionsBuilder<AuditDbContext>().UseInMemoryDatabase(dbName).Options;
        public AuditDbContext CreateDbContext() => new(_options);
        public Task<AuditDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult(CreateDbContext());
    }

    private static (SqlAuditSink Sink, InMemoryFactory Factory) BuildInner()
    {
        var factory = new InMemoryFactory("audit-" + Guid.NewGuid());
        return (new SqlAuditSink(factory), factory);
    }

    private static AuditEntry Entry() => new()
    {
        AgentId = "ai-assistant", UserId = "u1", UserRole = "Manager",
        ToolName = "set_box_status", Decision = AuthDecision.Allow, DecisionReason = "OK",
        Outcome = AuditOutcome.NotExecuted,
    };

    [Fact]
    public async Task RecordAsync_persists_the_entry()
    {
        var (sink, factory) = BuildInner();
        var entry = Entry();

        await sink.RecordAsync(entry);

        await using var db = factory.CreateDbContext();
        var stored = Assert.Single(db.AuditEntries);
        Assert.Equal(entry.Id, stored.Id);
        Assert.Equal("set_box_status", stored.ToolName);
        Assert.Equal(AuditOutcome.NotExecuted, stored.Outcome);
    }

    [Fact]
    public async Task UpdateOutcomeAsync_enriches_the_row_in_place()
    {
        var (sink, factory) = BuildInner();
        var entry = Entry();
        await sink.RecordAsync(entry);

        await sink.UpdateOutcomeAsync(entry.Id, AuditOutcome.Error, 55, "odbijeno (status 409)");

        await using var db = factory.CreateDbContext();
        var stored = Assert.Single(db.AuditEntries);
        Assert.Equal(AuditOutcome.Error, stored.Outcome);
        Assert.Equal(55, stored.DurationMs);
        Assert.Equal("odbijeno (status 409)", stored.Error);
        Assert.Equal("set_box_status", stored.ToolName); // untouched
    }

    [Fact]
    public async Task UpdateOutcomeAsync_for_unknown_id_is_a_noop()
    {
        var (sink, _) = BuildInner();

        await sink.UpdateOutcomeAsync(Guid.NewGuid(), AuditOutcome.Success, 1, null); // must not throw
    }
}
