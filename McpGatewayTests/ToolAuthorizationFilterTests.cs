using System.Security.Cryptography;
using McpGateway.Audit;
using McpGateway.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;
using Xunit;

namespace McpGatewayTests;

public class ToolAuthorizationFilterTests
{
    private const string AgentId = "ai-assistant";

    private static readonly ToolPolicy[] Tools =
    [
        new("get_problem_stats", ["Admin", "Manager"], OrgScoped: true, IsWrite: false),
    ];

    private static (ToolAuthorizationFilter Filter, InMemoryAuditSink Audit, RSA Key) Build()
    {
        var key = RSA.Create(2048);
        var agents = new[] { new AgentPolicy(AgentId, key.ExportSubjectPublicKeyInfoPem(), ["get_problem_stats"]) };
        var store = new PolicyStore(Tools, agents);
        var audit = new InMemoryAuditSink();
        var gk = new RequestGatekeeper(
            new AgentTokenValidator(store.Agents), new ToolAuthorizer(store.ToolPolicies), store, audit,
            NullLogger<RequestGatekeeper>.Instance);
        return (new ToolAuthorizationFilter(gk), audit, key);
    }

    private static CallToolResult ToolRan() => new() { Content = [new TextContentBlock { Text = "ran" }] };

    [Fact]
    public async Task Allow_forwards_to_the_tool()
    {
        var (filter, _, key) = Build();
        var token = TestAuth.MintAgentToken(key, AgentId, DateTime.UtcNow.AddMinutes(5));
        var user = TestAuth.Principal("u1", "Manager", Guid.NewGuid());
        var nextCalled = false;

        var result = await filter.EvaluateAsync(token, user, "get_problem_stats", null,
            next: _ => { nextCalled = true; return ValueTask.FromResult(ToolRan()); },
            CancellationToken.None);

        Assert.True(nextCalled);            // the real tool ran
        Assert.True(result.IsError != true); // not an error result
    }

    [Fact]
    public async Task Deny_short_circuits_with_error_and_skips_the_tool()
    {
        var (filter, audit, key) = Build();
        var token = TestAuth.MintAgentToken(key, AgentId, DateTime.UtcNow.AddMinutes(5));
        var user = TestAuth.Principal("u1", "Manager", Guid.NewGuid());
        var nextCalled = false;

        // The agent is not permitted "create_box" → gatekeeper denies before the tool runs.
        var result = await filter.EvaluateAsync(token, user, "create_box", null,
            next: _ => { nextCalled = true; return ValueTask.FromResult(ToolRan()); },
            CancellationToken.None);

        Assert.False(nextCalled);           // tool NOT executed
        Assert.True(result.IsError == true); // error result returned to the caller
        Assert.Single(audit.Entries);        // the deny was audited
    }
}
