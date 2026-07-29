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

    // ── fail-open guards ───────────────────────────────────────────────────────────
    //
    // ToolScope.Resolve treats a null EffectiveOrganizationId as "admin — global scope", and
    // WriteTools.BoxInScopeAsync returns true unconditionally in that case. ToolAuthorizer only
    // populates EffectiveOrganizationId for OrgScoped tools. So a tool configured
    // { IsWrite: true, OrgScoped: false } would let a MANAGER write to any box in ANY organization
    // — a real cross-org fail-open produced by one config token, with no code change.
    //
    // The same null sentinel means both "admin/global" and "not org-scoped", so the write path
    // cannot tell them apart at runtime. These guards make a mis-scoped policy a BOOT FAILURE
    // instead, which is the only place the distinction is still recoverable.

    [Fact]
    public void Throws_when_a_write_tool_is_not_org_scoped()
    {
        ToolPolicy[] tools = [new("set_box_status", ["Admin", "Manager"], OrgScoped: false, IsWrite: true)];

        var ex = Assert.Throws<InvalidOperationException>(
            () => new PolicyStore(tools, Array.Empty<AgentPolicy>()));

        Assert.Contains("set_box_status", ex.Message);
    }

    [Fact]
    public void Throws_when_a_non_org_scoped_tool_is_granted_to_a_non_admin_role()
    {
        // A non-org-scoped READ granted to Manager resolves the caller-supplied organizationId,
        // i.e. the LLM chooses the scope. Only Admin may hold a non-org-scoped tool.
        ToolPolicy[] tools = [new("get_global_stats", ["Admin", "Manager"], OrgScoped: false, IsWrite: false)];

        var ex = Assert.Throws<InvalidOperationException>(
            () => new PolicyStore(tools, Array.Empty<AgentPolicy>()));

        Assert.Contains("get_global_stats", ex.Message);
    }

    [Fact]
    public void Accepts_an_admin_only_non_org_scoped_read()
    {
        // The shipped get_global_stats shape: Admin-only + not org-scoped + read. Must stay legal.
        ToolPolicy[] tools = [new("get_global_stats", ["Admin"], OrgScoped: false, IsWrite: false)];

        var store = new PolicyStore(tools, Array.Empty<AgentPolicy>());

        Assert.True(store.ToolPolicies.ContainsKey("get_global_stats"));
    }

    [Fact]
    public void Accepts_the_shipped_production_policy_set()
    {
        // Regression guard: whatever the guards forbid, they must not forbid what actually ships.
        ToolPolicy[] shipped =
        [
            new("get_org_overview",         ["Admin", "Manager"], OrgScoped: true,  IsWrite: false),
            new("get_global_stats",         ["Admin"],            OrgScoped: false, IsWrite: false),
            new("set_box_status",           ["Admin", "Manager"], OrgScoped: true,  IsWrite: true),
            new("set_box_password",         ["Admin", "Manager"], OrgScoped: true,  IsWrite: true),
            new("create_box",               ["Admin", "Manager"], OrgScoped: true,  IsWrite: true),
            new("add_comment",              ["Admin", "Manager"], OrgScoped: true,  IsWrite: true),
            new("update_submission_status", ["Admin", "Manager"], OrgScoped: true,  IsWrite: true),
        ];

        var store = new PolicyStore(shipped, Array.Empty<AgentPolicy>());

        Assert.Equal(7, store.ToolPolicies.Count);
    }
}
