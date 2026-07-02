using System.Security.Cryptography;
using McpGateway.Audit;
using McpGateway.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpGatewayTests;

public class RequestGatekeeperTests
{
    private const string AgentId = "ai-assistant";

    private static readonly ToolPolicy[] Tools =
    [
        new("get_problem_stats", ["Admin", "Manager"], OrgScoped: true,  IsWrite: false),
        new("get_global_stats",  ["Admin"],            OrgScoped: false, IsWrite: false),
        new("set_box_status",    ["Admin", "Manager"], OrgScoped: true,  IsWrite: true),
    ];

    private static (RequestGatekeeper Gk, InMemoryAuditSink Audit, RSA Key) Build(params string[] agentTools)
    {
        var key = RSA.Create(2048);
        var agents = new[] { new AgentPolicy(AgentId, key.ExportSubjectPublicKeyInfoPem(), agentTools) };
        var store = new PolicyStore(Tools, agents);
        var audit = new InMemoryAuditSink();
        var gk = new RequestGatekeeper(
            new AgentTokenValidator(store.Agents),
            new ToolAuthorizer(store.ToolPolicies),
            store,
            audit,
            NullLogger<RequestGatekeeper>.Instance);
        return (gk, audit, key);
    }

    [Fact]
    public async Task Allows_and_audits_a_permitted_call()
    {
        var (gk, audit, key) = Build("get_problem_stats");
        var token = TestAuth.MintAgentToken(key, AgentId, DateTime.UtcNow.AddMinutes(5));
        var user = TestAuth.Principal("u1", "Manager", Guid.NewGuid());

        var result = await gk.AuthorizeAndAuditAsync(token, user, "get_problem_stats");

        Assert.Equal(AuthDecision.Allow, result.Decision);
        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AuthDecision.Allow, entry.Decision);
        Assert.Equal(AgentId, entry.AgentId);
        Assert.Equal("u1", entry.UserId);
    }

    [Fact]
    public async Task Denies_and_audits_when_agent_token_invalid()
    {
        var (gk, audit, _) = Build("get_problem_stats");
        var user = TestAuth.Principal("u1", "Manager", Guid.NewGuid());

        var result = await gk.AuthorizeAndAuditAsync("not-a-token", user, "get_problem_stats");

        Assert.Equal(AuthDecision.Deny, result.Decision);
        Assert.Equal(AuthDecision.Deny, Assert.Single(audit.Entries).Decision);
    }

    [Fact]
    public async Task Denies_and_audits_when_user_not_authenticated()
    {
        var (gk, audit, key) = Build("get_problem_stats");
        var token = TestAuth.MintAgentToken(key, AgentId, DateTime.UtcNow.AddMinutes(5));

        var result = await gk.AuthorizeAndAuditAsync(token, user: null, "get_problem_stats");

        Assert.Equal(AuthDecision.Deny, result.Decision);
        Assert.Single(audit.Entries);
    }

    [Fact] // Manager -> admin-only tool
    public async Task Denies_and_audits_role_violation()
    {
        var (gk, audit, key) = Build("get_global_stats");
        var token = TestAuth.MintAgentToken(key, AgentId, DateTime.UtcNow.AddMinutes(5));
        var user = TestAuth.Principal("u1", "Manager", Guid.NewGuid());

        var result = await gk.AuthorizeAndAuditAsync(token, user, "get_global_stats");

        Assert.Equal(AuthDecision.Deny, result.Decision);
        Assert.Contains("Manager", Assert.Single(audit.Entries).DecisionReason);
    }

    [Fact] // write tool authorized -> recorded as Proposed (human-in-the-loop)
    public async Task Marks_authorized_write_as_proposed_in_audit()
    {
        var (gk, audit, key) = Build("set_box_status");
        var token = TestAuth.MintAgentToken(key, AgentId, DateTime.UtcNow.AddMinutes(5));
        var user = TestAuth.Principal("u1", "Manager", Guid.NewGuid());

        var result = await gk.AuthorizeAndAuditAsync(token, user, "set_box_status");

        Assert.Equal(AuthDecision.Allow, result.Decision);
        var entry = Assert.Single(audit.Entries);
        Assert.True(entry.IsWrite);
        Assert.Equal(ConfirmationState.Proposed, entry.Confirmation);
    }
}
