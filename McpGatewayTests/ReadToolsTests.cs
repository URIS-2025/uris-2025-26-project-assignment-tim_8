using System.Text.Json;
using McpGateway.Authorization;
using McpGateway.Data;
using McpGateway.Tools;
using Moq;
using Xunit;

namespace McpGatewayTests;

/// <summary>
/// Read-tool bodies with a mocked <see cref="IReadRepository"/> (EF InMemory can't run the views).
/// Proves the security-critical orchestration: a manager is forced to their own org, an admin may
/// target a requested org (or all), out-of-scope lookups leak nothing, and the DTOs carry no PII.
/// </summary>
public class ReadToolsTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static IInvocationContextAccessor Ctx(Guid? effectiveOrg, string role) =>
        new InvocationContextAccessor { Current = new InvocationContext(effectiveOrg, role, "u1", null) };

    private static Mock<IReadRepository> Repo()
    {
        var m = new Mock<IReadRepository>(MockBehavior.Loose);
        m.Setup(r => r.GetProblemBoxIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Guid.NewGuid() } as IReadOnlyList<Guid>);
        m.Setup(r => r.GetSuggestionBoxIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guid>() as IReadOnlyList<Guid>);
        m.Setup(r => r.GetProblemStatsAsync(It.IsAny<IReadOnlyList<Guid>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProblemStatsDto(0, [], []));
        return m;
    }

    [Fact]
    public async Task GetProblemStats_manager_is_forced_to_own_org_ignoring_arg()
    {
        var own = Guid.NewGuid();
        var attackerOrg = Guid.NewGuid();
        var repo = Repo();

        await ReadTools.GetProblemStats(repo.Object, Ctx(own, "Manager"), organizationId: attackerOrg);

        repo.Verify(r => r.GetProblemBoxIdsAsync(own, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.GetProblemBoxIdsAsync(attackerOrg, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetProblemStats_admin_targets_requested_org()
    {
        var target = Guid.NewGuid();
        var repo = Repo();

        await ReadTools.GetProblemStats(repo.Object, Ctx(null, "Admin"), organizationId: target);

        repo.Verify(r => r.GetProblemBoxIdsAsync(target, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProblemStats_admin_without_org_queries_all_without_resolving_boxes()
    {
        var repo = Repo();

        await ReadTools.GetProblemStats(repo.Object, Ctx(null, "Admin"), organizationId: null);

        repo.Verify(r => r.GetProblemBoxIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.GetProblemStatsAsync(It.Is<IReadOnlyList<Guid>?>(x => x == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListBoxes_manager_is_scoped_to_own_org()
    {
        var own = Guid.NewGuid();
        var repo = Repo();
        repo.Setup(r => r.ListBoxesAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<BoxDto>());

        await ReadTools.ListBoxes(repo.Object, Ctx(own, "Manager"), organizationId: Guid.NewGuid());

        repo.Verify(r => r.ListBoxesAsync(own, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSubmissionDetails_out_of_scope_returns_error_and_leaks_nothing()
    {
        var repo = Repo();
        // repo enforces scope in the query and returns null for a foreign / missing submission.
        repo.Setup(r => r.GetProblemDetailsAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Guid>?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync((SubmissionDetailsDto?)null);

        var json = await ReadTools.GetSubmissionDetails(
            repo.Object, Ctx(Guid.NewGuid(), "Manager"), type: "problem", id: Guid.NewGuid());

        Assert.Contains("error", json);
        Assert.DoesNotContain("Title", json); // no submission fields leaked
    }

    [Fact]
    public async Task GetSubmissionDetails_unknown_type_returns_error()
    {
        var json = await ReadTools.GetSubmissionDetails(
            Repo().Object, Ctx(Guid.NewGuid(), "Manager"), type: "banana", id: Guid.NewGuid());

        Assert.Contains("error", json);
    }

    [Fact]
    public async Task GetProblemStats_missing_context_fails_closed()
    {
        var repo = Repo();
        var noCtx = new InvocationContextAccessor(); // Current == null

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ReadTools.GetProblemStats(repo.Object, noCtx));
    }

    [Fact]
    public async Task SearchSubmissions_caps_merged_results_at_the_safety_ceiling()
    {
        static IReadOnlyList<SubmissionSummaryDto> Many(string type, int n) =>
            Enumerable.Range(0, n)
                .Select(_ => new SubmissionSummaryDto(Guid.NewGuid(), type, "t", 0, DateTime.UtcNow))
                .ToList();

        var repo = new Mock<IReadRepository>(MockBehavior.Loose);
        // Admin (no org) → both sources searched unscoped; each returns a full cap of 100.
        repo.Setup(r => r.SearchProblemsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<Guid>?>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(Many("problem", 100));
        repo.Setup(r => r.SearchSuggestionsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<Guid>?>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(Many("suggestion", 100));

        var json = await ReadTools.SearchSubmissions(repo.Object, Ctx(null, "Admin"), keyword: "x");
        var results = JsonSerializer.Deserialize<List<SubmissionSummaryDto>>(json, Json)!;

        Assert.Equal(100, results.Count); // merged ceiling = SearchCap, not 2×cap
    }

    [Fact]
    public void Read_dtos_expose_no_pii_properties()
    {
        Type[] dtos =
        [
            typeof(OrgOverviewDto), typeof(CodeCount), typeof(ProblemStatsDto), typeof(SuggestionStatsDto),
            typeof(BoxDto), typeof(SubmissionSummaryDto), typeof(CommentDto), typeof(SubmissionDetailsDto),
            typeof(GlobalStatsDto),
        ];
        string[] forbidden = ["author", "anonymoususer", "createdby", "password", "token", "hash"];

        foreach (var dto in dtos)
            foreach (var prop in dto.GetProperties())
            {
                var name = prop.Name.ToLowerInvariant();
                if (name == "haspassword") continue; // deliberate scrubbed boolean flag — NOT the secret
                Assert.DoesNotContain(forbidden, f => name.Contains(f));
            }
    }
}
