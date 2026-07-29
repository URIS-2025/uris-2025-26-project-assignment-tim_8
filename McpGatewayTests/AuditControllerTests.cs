using System.Security.Claims;
using McpGateway.Audit;
using McpGateway.Authorization;
using McpGateway.Context;
using McpGateway.Controllers;
using McpGateway.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpGatewayTests;

/// <summary>
/// <see cref="AuditController"/> query logic over an EF InMemory store. Proves the force-org security
/// rule (a manager sees ONLY their org, a client-supplied org is ignored, null-org records are
/// admin-only), the filters, the page-size cap, and that a scrubbed DTO — not the entity — is returned.
/// </summary>
public class AuditControllerTests
{
    private sealed class InMemoryFactory : IDbContextFactory<AuditDbContext>
    {
        private readonly DbContextOptions<AuditDbContext> _options;
        public InMemoryFactory(string dbName) =>
            _options = new DbContextOptionsBuilder<AuditDbContext>().UseInMemoryDatabase(dbName).Options;
        public AuditDbContext CreateDbContext() => new(_options);
        public Task<AuditDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult(CreateDbContext());
    }

    private static AuditEntry Entry(Guid? org, string tool = "get_org_overview",
        AuthDecision decision = AuthDecision.Allow, AuditOutcome outcome = AuditOutcome.Success) => new()
    {
        AgentId = "ai-assistant", UserId = "u1", UserRole = "Manager", OrganizationId = org,
        ToolName = tool, Decision = decision, DecisionReason = "OK", Outcome = outcome,
    };

    private static AuditController ControllerFor(IDbContextFactory<AuditDbContext> factory, string role, Guid? org)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "u1"), new(ClaimTypes.Role, role) };
        if (org is not null) claims.Add(new Claim("OrganizationId", org.ToString()!));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        return new AuditController(factory)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } },
        };
    }

    private static async Task<InMemoryFactory> Seed(params AuditEntry[] entries)
    {
        var factory = new InMemoryFactory("audit-" + Guid.NewGuid());
        await using var db = factory.CreateDbContext();
        db.AuditEntries.AddRange(entries);
        await db.SaveChangesAsync();
        return factory;
    }

    private static AuditPageDTO Page(ActionResult<AuditPageDTO> result) =>
        Assert.IsType<AuditPageDTO>(Assert.IsType<OkObjectResult>(result.Result).Value);

    [Fact]
    public async Task Manager_sees_only_own_org_ignoring_supplied_org_and_null_org()
    {
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var factory = await Seed(Entry(orgA), Entry(orgB), Entry(null, decision: AuthDecision.Deny));
        var controller = ControllerFor(factory, "Manager", orgA);

        // Manager tries to widen scope to orgB — must be ignored.
        var page = Page(await controller.GetAudit(organizationId: orgB));

        Assert.Equal(1, page.Total);
        Assert.All(page.Items, i => Assert.Equal(orgA, i.OrganizationId));
    }

    [Fact]
    public async Task Admin_sees_all_orgs_including_null_org_records()
    {
        var orgA = Guid.NewGuid();
        var factory = await Seed(Entry(orgA), Entry(Guid.NewGuid()), Entry(null, decision: AuthDecision.Deny));
        var controller = ControllerFor(factory, "Admin", null);

        var page = Page(await controller.GetAudit());

        Assert.Equal(3, page.Total);
    }

    [Fact] // Admin role is matched case-insensitively (UserRole.Title is free-form) — same rule as ToolAuthorizer.
    public async Task Lowercase_admin_role_is_treated_as_admin()
    {
        var factory = await Seed(Entry(Guid.NewGuid()), Entry(Guid.NewGuid()), Entry(null, decision: AuthDecision.Deny));
        var controller = ControllerFor(factory, "admin", null); // lowercase title

        var page = Page(await controller.GetAudit());

        Assert.Equal(3, page.Total); // sees all orgs incl null-org → treated as admin, not org-pinned
    }

    [Fact]
    public async Task Admin_can_filter_by_one_org()
    {
        var orgA = Guid.NewGuid();
        var factory = await Seed(Entry(orgA), Entry(Guid.NewGuid()));
        var controller = ControllerFor(factory, "Admin", null);

        var page = Page(await controller.GetAudit(organizationId: orgA));

        Assert.Equal(1, page.Total);
        Assert.Equal(orgA, Assert.Single(page.Items).OrganizationId);
    }

    [Fact]
    public async Task Filters_by_tool_and_outcome()
    {
        var org = Guid.NewGuid();
        var factory = await Seed(
            Entry(org, tool: "set_box_status", outcome: AuditOutcome.Error),
            Entry(org, tool: "get_org_overview", outcome: AuditOutcome.Success));
        var controller = ControllerFor(factory, "Admin", null);

        var byTool = Page(await controller.GetAudit(toolName: "set_box_status"));
        var byOutcome = Page(await controller.GetAudit(outcome: AuditOutcome.Error));

        Assert.Equal("set_box_status", Assert.Single(byTool.Items).ToolName);
        Assert.Equal(AuditOutcome.Error, Assert.Single(byOutcome.Items).Outcome);
    }

    // The tool filter is a SUBSTRING match, not an exact one. The catalog is snake_case with shared
    // prefixes (list_boxes, get_org_overview, set_box_status, set_box_password), so an operator
    // reviewing the audit types a fragment — "list_", "set_box" — to see a family of calls. Exact
    // matching made the filter unusable unless you already knew the full tool name.
    [Fact]
    public async Task ToolName_filter_matches_a_prefix_fragment()
    {
        var org = Guid.NewGuid();
        var factory = await Seed(
            Entry(org, "list_boxes"),
            Entry(org, "get_org_overview"),
            Entry(org, "set_box_status"));
        var controller = ControllerFor(factory, "Admin", null);

        var page = Page(await controller.GetAudit(toolName: "list_"));

        Assert.Equal("list_boxes", Assert.Single(page.Items).ToolName);
    }

    [Fact]
    public async Task ToolName_filter_matches_a_shared_fragment_across_several_tools()
    {
        var org = Guid.NewGuid();
        var factory = await Seed(
            Entry(org, "set_box_status"),
            Entry(org, "set_box_password"),
            Entry(org, "list_boxes"));
        var controller = ControllerFor(factory, "Admin", null);

        var page = Page(await controller.GetAudit(toolName: "set_box"));

        Assert.Equal(2, page.Items.Count);
        Assert.All(page.Items, i => Assert.StartsWith("set_box", i.ToolName));
    }

    [Fact]
    public async Task ToolName_filter_ignores_surrounding_whitespace()
    {
        var org = Guid.NewGuid();
        var factory = await Seed(Entry(org, "list_boxes"), Entry(org, "get_org_overview"));
        var controller = ControllerFor(factory, "Admin", null);

        var page = Page(await controller.GetAudit(toolName: "  list_  "));

        Assert.Equal("list_boxes", Assert.Single(page.Items).ToolName);
    }

    [Fact]
    public async Task PageSize_is_capped_at_100()
    {
        var org = Guid.NewGuid();
        var many = Enumerable.Range(0, 150).Select(_ => Entry(org)).ToArray();
        var factory = await Seed(many);
        var controller = ControllerFor(factory, "Admin", null);

        var page = Page(await controller.GetAudit(pageSize: 1000)); // request over the cap

        Assert.Equal(150, page.Total);        // total reflects all matches
        Assert.Equal(100, page.Items.Count);  // but a single page is capped at 100
    }
}
