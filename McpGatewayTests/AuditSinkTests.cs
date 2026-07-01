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
}
