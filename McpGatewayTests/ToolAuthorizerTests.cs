using McpGateway.Authorization;
using Xunit;

namespace McpGatewayTests;

public class ToolAuthorizerTests
{
    // The slice of the 12-tool catalog exercised by these tests.
    private static ToolAuthorizer BuildAuthorizer() => new(new Dictionary<string, ToolPolicy>
    {
        ["get_problem_stats"] = new("get_problem_stats", ["Admin", "Manager"], OrgScoped: true,  IsWrite: false),
        ["get_global_stats"]  = new("get_global_stats",  ["Admin"],            OrgScoped: false, IsWrite: false),
    });

    [Fact]
    public void Allows_manager_on_permitted_org_scoped_read_and_locks_to_their_org()
    {
        var authorizer = BuildAuthorizer();
        var orgId = Guid.NewGuid();
        var agent = new AgentContext("ai-assistant", new HashSet<string> { "get_problem_stats" });
        var user = new UserContext("user-1", "Manager", orgId);

        var result = authorizer.Authorize("get_problem_stats", agent, user);

        Assert.Equal(AuthDecision.Allow, result.Decision);
        Assert.Equal(orgId, result.EffectiveOrganizationId); // scoped to the manager's OWN org
    }

    [Fact] // T2 — agent dimension of dual-principal
    public void Denies_when_agent_is_not_permitted_the_tool()
    {
        var authorizer = BuildAuthorizer();
        // This agent may ONLY call get_global_stats, but the user asks for get_problem_stats.
        var agent = new AgentContext("ai-assistant", new HashSet<string> { "get_global_stats" });
        var user = new UserContext("user-1", "Manager", Guid.NewGuid());

        var result = authorizer.Authorize("get_problem_stats", agent, user);

        Assert.Equal(AuthDecision.Deny, result.Decision);
        Assert.Contains("agent", result.Reason);
    }

    [Fact] // T3 — user-role dimension: Manager hits an Admin-only tool
    public void Denies_when_user_role_is_not_permitted()
    {
        var authorizer = BuildAuthorizer();
        var agent = new AgentContext("ai-assistant", new HashSet<string> { "get_global_stats" });
        var user = new UserContext("user-1", "Manager", Guid.NewGuid());

        var result = authorizer.Authorize("get_global_stats", agent, user);

        Assert.Equal(AuthDecision.Deny, result.Decision);
        Assert.Contains("Manager", result.Reason);
    }

    [Fact] // T4 — org-scoped tool but the manager has no org in the token
    public void Denies_org_scoped_tool_when_user_has_no_organization()
    {
        var authorizer = BuildAuthorizer();
        var agent = new AgentContext("ai-assistant", new HashSet<string> { "get_problem_stats" });
        var user = new UserContext("user-1", "Manager", OrganizationId: null);

        var result = authorizer.Authorize("get_problem_stats", agent, user);

        Assert.Equal(AuthDecision.Deny, result.Decision);
    }

    [Fact] // T5 — Admin allowed on the admin-only, global tool
    public void Allows_admin_on_admin_only_global_tool_without_org_scope()
    {
        var authorizer = BuildAuthorizer();
        var agent = new AgentContext("ai-assistant", new HashSet<string> { "get_global_stats" });
        var user = new UserContext("admin-1", "Admin", OrganizationId: null);

        var result = authorizer.Authorize("get_global_stats", agent, user);

        Assert.Equal(AuthDecision.Allow, result.Decision);
        Assert.Null(result.EffectiveOrganizationId); // global — not scoped
    }

    [Fact] // Admin on an org-scoped tool is NOT locked to a single org (global)
    public void Allows_admin_on_org_scoped_tool_without_locking_to_an_org()
    {
        var authorizer = BuildAuthorizer();
        var agent = new AgentContext("ai-assistant", new HashSet<string> { "get_problem_stats" });
        var user = new UserContext("admin-1", "Admin", OrganizationId: null);

        var result = authorizer.Authorize("get_problem_stats", agent, user);

        Assert.Equal(AuthDecision.Allow, result.Decision);
        Assert.Null(result.EffectiveOrganizationId);
    }

    [Fact] // Unknown / unregistered tool is denied
    public void Denies_unknown_tool()
    {
        var authorizer = BuildAuthorizer();
        var agent = new AgentContext("ai-assistant", new HashSet<string> { "does_not_exist" });
        var user = new UserContext("user-1", "Manager", Guid.NewGuid());

        var result = authorizer.Authorize("does_not_exist", agent, user);

        Assert.Equal(AuthDecision.Deny, result.Decision);
    }

    [Fact] // role titles are free-form (UserRole.Title) → matching is case-insensitive
    public void Allows_manager_with_lowercase_role_title()
    {
        var authorizer = BuildAuthorizer();
        var orgId = Guid.NewGuid();
        var agent = new AgentContext("ai-assistant", new HashSet<string> { "get_problem_stats" });
        var user = new UserContext("user-1", "manager", orgId); // lowercase title

        var result = authorizer.Authorize("get_problem_stats", agent, user);

        Assert.Equal(AuthDecision.Allow, result.Decision);
        Assert.Equal(orgId, result.EffectiveOrganizationId);
    }

    [Fact] // the admin-is-global branch must also be case-insensitive
    public void Treats_lowercase_admin_as_global()
    {
        var authorizer = BuildAuthorizer();
        var agent = new AgentContext("ai-assistant", new HashSet<string> { "get_problem_stats" });
        var user = new UserContext("admin-1", "admin", OrganizationId: null); // lowercase, no org

        var result = authorizer.Authorize("get_problem_stats", agent, user);

        Assert.Equal(AuthDecision.Allow, result.Decision);
        Assert.Null(result.EffectiveOrganizationId); // admin → global, not org-denied
    }
}
