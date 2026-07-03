using McpGateway.Audit;
using McpGateway.Authorization;
using Xunit;

namespace McpGatewayTests;

public class AuditSinkTests
{
    [Fact]
    public async Task Records_and_exposes_the_entry()
    {
        var sink = new InMemoryAuditSink();
        var entry = new AuditEntry
        {
            AgentId = "ai-assistant",
            UserId = "u1",
            UserRole = "Manager",
            ToolName = "get_org_overview",
            Decision = AuthDecision.Allow,
            DecisionReason = "OK",
            Outcome = AuditOutcome.Success,
        };

        await sink.RecordAsync(entry);

        Assert.Single(sink.Entries);
        Assert.Equal("get_org_overview", sink.Entries[0].ToolName);
    }

    [Fact]
    public async Task UpdateOutcome_enriches_the_recorded_entry_in_place()
    {
        var sink = new InMemoryAuditSink();
        var entry = new AuditEntry
        {
            AgentId = "ai-assistant", UserId = "u1", UserRole = "Manager",
            ToolName = "set_box_status", Decision = AuthDecision.Allow, DecisionReason = "OK",
            Outcome = AuditOutcome.NotExecuted, // decision-level record, not yet executed
        };
        await sink.RecordAsync(entry);

        await sink.UpdateOutcomeAsync(entry.Id, AuditOutcome.Error, durationMs: 42, error: "odbijeno (status 409)");

        var updated = Assert.Single(sink.Entries);
        Assert.Equal(entry.Id, updated.Id);
        Assert.Equal(AuditOutcome.Error, updated.Outcome);
        Assert.Equal(42, updated.DurationMs);
        Assert.Equal("odbijeno (status 409)", updated.Error);
        Assert.Equal("set_box_status", updated.ToolName); // other fields preserved
    }

    [Fact]
    public async Task UpdateOutcome_for_unknown_id_is_a_noop()
    {
        var sink = new InMemoryAuditSink();

        // Best-effort: enriching a non-existent id must NOT throw (the action already happened).
        await sink.UpdateOutcomeAsync(Guid.NewGuid(), AuditOutcome.Success, 1, null);

        Assert.Empty(sink.Entries);
    }
}
