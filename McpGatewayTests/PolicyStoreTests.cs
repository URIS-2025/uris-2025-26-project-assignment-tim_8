using McpGateway.Authorization;
using Xunit;

namespace McpGatewayTests;

public class PolicyStoreTests
{
    private static readonly ToolPolicy[] Tools =
    [
        new("get_org_overview", ["Admin", "Manager"], OrgScoped: true,  IsWrite: false),
        new("get_global_stats", ["Admin"],            OrgScoped: false, IsWrite: false),
    ];

    [Fact]
    public void Exposes_tool_policies_and_agents_for_valid_config()
    {
        var agents = new[] { new AgentPolicy("ai-assistant", "PEM", ["get_org_overview"]) };

        var store = new PolicyStore(Tools, agents);

        Assert.True(store.ToolPolicies.ContainsKey("get_global_stats"));
        Assert.Single(store.Agents);
    }

    [Fact] // fail-fast: an agent may not reference a tool that does not exist
    public void Throws_when_agent_references_an_unknown_tool()
    {
        var agents = new[] { new AgentPolicy("ai-assistant", "PEM", ["does_not_exist"]) };

        Assert.Throws<InvalidOperationException>(() => new PolicyStore(Tools, agents));
    }
}
