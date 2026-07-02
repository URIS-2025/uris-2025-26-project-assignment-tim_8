using McpGateway.Authorization;
using McpGateway.Tools;
using Xunit;

namespace McpGatewayTests;

/// <summary>The read-side org-scope guard: manager always locked to own org; admin optional target.</summary>
public class ToolScopeTests
{
    private static InvocationContext Ctx(Guid? effectiveOrg, string role) =>
        new(effectiveOrg, role, "u1", BearerToken: null);

    [Fact]
    public void Manager_is_forced_to_own_org_ignoring_requested_org()
    {
        var own = Guid.NewGuid();
        var someoneElse = Guid.NewGuid();

        var result = ToolScope.Resolve(Ctx(own, "Manager"), requestedOrganizationId: someoneElse);

        Assert.Equal(own, result); // requested org is ignored — no scope widening
    }

    [Fact]
    public void Manager_with_no_requested_org_still_scoped_to_own()
    {
        var own = Guid.NewGuid();
        Assert.Equal(own, ToolScope.Resolve(Ctx(own, "Manager"), requestedOrganizationId: null));
    }

    [Fact]
    public void Admin_may_target_a_requested_org()
    {
        var target = Guid.NewGuid();
        Assert.Equal(target, ToolScope.Resolve(Ctx(null, "Admin"), requestedOrganizationId: target));
    }

    [Fact]
    public void Admin_with_no_requested_org_gets_all_orgs()
    {
        Assert.Null(ToolScope.Resolve(Ctx(null, "Admin"), requestedOrganizationId: null));
    }

    [Fact]
    public void Missing_context_fails_closed()
    {
        Assert.Throws<InvalidOperationException>(() => ToolScope.Resolve(null, Guid.NewGuid()));
    }
}
