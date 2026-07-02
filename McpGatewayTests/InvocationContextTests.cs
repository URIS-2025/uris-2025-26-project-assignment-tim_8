using System.Security.Cryptography;
using McpGateway.Audit;
using McpGateway.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;
using Xunit;

namespace McpGatewayTests;

/// <summary>
/// Verifies the shared infra that carries the authorized execution context from the authorization
/// filter to the tool body: on allow it is populated (with the FORCED org for a manager, so a read
/// tool cannot be tricked into another org); on deny it is never set.
/// </summary>
public class InvocationContextTests
{
    private const string AgentId = "ai-assistant";

    private static readonly ToolPolicy[] Tools =
    [
        new("get_problem_stats", ["Admin", "Manager"], OrgScoped: true, IsWrite: false),
    ];

    private static (ToolAuthorizationFilter Filter, RSA Key) Build()
    {
        var key = RSA.Create(2048);
        var agents = new[] { new AgentPolicy(AgentId, key.ExportSubjectPublicKeyInfoPem(), ["get_problem_stats"]) };
        var store = new PolicyStore(Tools, agents);
        var gk = new RequestGatekeeper(
            new AgentTokenValidator(store.Agents), new ToolAuthorizer(store.ToolPolicies), store,
            new InMemoryAuditSink(), NullLogger<RequestGatekeeper>.Instance);
        return (new ToolAuthorizationFilter(gk), key);
    }

    private static CallToolResult ToolRan() => new() { Content = [new TextContentBlock { Text = "ran" }] };

    [Fact]
    public async Task Allow_populates_context_with_forced_org_role_and_bearer()
    {
        var (filter, key) = Build();
        var token = TestAuth.MintAgentToken(key, AgentId, DateTime.UtcNow.AddMinutes(5));
        var org = Guid.NewGuid();
        var user = TestAuth.Principal("u1", "Manager", org);
        var accessor = new InvocationContextAccessor();

        var result = await filter.EvaluateAsync(token, user, "get_problem_stats", null,
            next: _ => ValueTask.FromResult(ToolRan()), CancellationToken.None,
            invocationContext: accessor, bearerToken: "Bearer xyz");

        Assert.True(result.IsError != true);
        Assert.NotNull(accessor.Current);
        Assert.Equal(org, accessor.Current!.EffectiveOrganizationId); // forced to the manager's org
        Assert.Equal("Manager", accessor.Current.UserRole);
        Assert.Equal("u1", accessor.Current.UserId);
        Assert.Equal("Bearer xyz", accessor.Current.BearerToken);
    }

    [Fact]
    public async Task Deny_never_sets_the_context()
    {
        var (filter, key) = Build();
        var token = TestAuth.MintAgentToken(key, AgentId, DateTime.UtcNow.AddMinutes(5));
        var user = TestAuth.Principal("u1", "Manager", Guid.NewGuid());
        var accessor = new InvocationContextAccessor();

        // Agent lacks "create_box" → deny before any tool/context is set.
        var result = await filter.EvaluateAsync(token, user, "create_box", null,
            next: _ => ValueTask.FromResult(ToolRan()), CancellationToken.None,
            invocationContext: accessor, bearerToken: "Bearer xyz");

        Assert.True(result.IsError == true);
        Assert.Null(accessor.Current);
    }
}
