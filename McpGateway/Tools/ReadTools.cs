using System.ComponentModel;
using System.Text.Json;
using McpGateway.Authorization;
using McpGateway.Data;
using ModelContextProtocol.Server;

namespace McpGateway.Tools;

/// <summary>
/// Read tools (analytics + retrieval). Each reads only scrubbed, PII-free data through the
/// SELECT-only <see cref="IReadRepository"/>, and is org-scoped via <see cref="ToolScope"/>: a manager
/// is locked to their own org (the <c>organizationId</c> argument is ignored), while an admin may
/// optionally target one org (omit = all). Org-scope for submissions is composed here — resolve the
/// org's box ids, then query submissions by those ids — so the SQL never joins across databases.
/// Tool names match the policy keys in appsettings.
/// </summary>
[McpServerToolType]
public static class ReadTools
{
    /// <summary>Fixed safety cap on search results (non-negotiable; a tunable limit is deferred).</summary>
    private const int SearchCap = 100;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Json);

    private static Guid? Scope(IInvocationContextAccessor ctx, Guid? requestedOrganizationId)
        => ToolScope.Resolve(ctx.Current, requestedOrganizationId);

    [McpServerTool(Name = "get_org_overview"), Description("Aggregate counts of problems/suggestions/boxes for an organization.")]
    public static async Task<string> GetOrgOverview(
        IReadRepository repo, IInvocationContextAccessor ctx,
        [Description("Admin only: organization to target. Ignored for managers (own org). Omit for all orgs.")]
        Guid? organizationId = null,
        CancellationToken ct = default)
    {
        var org = Scope(ctx, organizationId);
        if (org is Guid o)
        {
            var pBoxes = await repo.GetProblemBoxIdsAsync(o, ct);
            var sBoxes = await repo.GetSuggestionBoxIdsAsync(o, ct);
            var overview = new OrgOverviewDto(
                await repo.CountProblemsAsync(pBoxes, ct),
                await repo.CountSuggestionsAsync(sBoxes, ct),
                await repo.CountProblemBoxesAsync(o, ct) + await repo.CountSuggestionBoxesAsync(o, ct));
            return Serialize(overview);
        }

        return Serialize(new OrgOverviewDto(
            await repo.CountProblemsAsync(null, ct),
            await repo.CountSuggestionsAsync(null, ct),
            await repo.CountProblemBoxesAsync(null, ct) + await repo.CountSuggestionBoxesAsync(null, ct)));
    }

    [McpServerTool(Name = "get_problem_stats"), Description("Problem counts grouped by status and priority for an organization.")]
    public static async Task<string> GetProblemStats(
        IReadRepository repo, IInvocationContextAccessor ctx,
        [Description("Admin only: organization to target. Ignored for managers. Omit for all orgs.")]
        Guid? organizationId = null,
        CancellationToken ct = default)
    {
        var org = Scope(ctx, organizationId);
        var boxIds = org is Guid o ? await repo.GetProblemBoxIdsAsync(o, ct) : null;
        return Serialize(await repo.GetProblemStatsAsync(boxIds, ct));
    }

    [McpServerTool(Name = "get_suggestion_stats"), Description("Suggestion counts by status plus total votes for an organization.")]
    public static async Task<string> GetSuggestionStats(
        IReadRepository repo, IInvocationContextAccessor ctx,
        [Description("Admin only: organization to target. Ignored for managers. Omit for all orgs.")]
        Guid? organizationId = null,
        CancellationToken ct = default)
    {
        var org = Scope(ctx, organizationId);
        var boxIds = org is Guid o ? await repo.GetSuggestionBoxIdsAsync(o, ct) : null;
        return Serialize(await repo.GetSuggestionStatsAsync(boxIds, ct));
    }

    [McpServerTool(Name = "list_boxes"), Description("List an organization's boxes (name/status/hasPassword; never the password).")]
    public static async Task<string> ListBoxes(
        IReadRepository repo, IInvocationContextAccessor ctx,
        [Description("Admin only: organization to target. Ignored for managers. Omit for all orgs.")]
        Guid? organizationId = null,
        CancellationToken ct = default)
    {
        var org = Scope(ctx, organizationId);
        return Serialize(await repo.ListBoxesAsync(org, ct));
    }

    [McpServerTool(Name = "search_submissions"), Description("Search problems and suggestions by keyword (scrubbed, no submitter identity).")]
    public static async Task<string> SearchSubmissions(
        IReadRepository repo, IInvocationContextAccessor ctx,
        [Description("Keyword matched against title/description.")] string keyword,
        [Description("Admin only: organization to target. Ignored for managers. Omit for all orgs.")]
        Guid? organizationId = null,
        CancellationToken ct = default)
    {
        var org = Scope(ctx, organizationId);
        var pBoxes = org is Guid po ? await repo.GetProblemBoxIdsAsync(po, ct) : null;
        var sBoxes = org is Guid so ? await repo.GetSuggestionBoxIdsAsync(so, ct) : null;

        var problems = await repo.SearchProblemsAsync(keyword, pBoxes, SearchCap, ct);
        var suggestions = await repo.SearchSuggestionsAsync(keyword, sBoxes, SearchCap, ct);
        // Each source is TOP-capped in SQL; bound the MERGED result too so the response can never
        // exceed SearchCap total (a single hard safety ceiling, not 2×cap).
        return Serialize(problems.Concat(suggestions).Take(SearchCap).ToList());
    }

    [McpServerTool(Name = "get_submission_details"), Description("Full details of one problem/suggestion plus its comments (scrubbed). Args: type ('problem'|'suggestion'), id.")]
    public static async Task<string> GetSubmissionDetails(
        IReadRepository repo, IInvocationContextAccessor ctx,
        [Description("'problem' or 'suggestion'.")] string type,
        [Description("The submission id.")] Guid id,
        [Description("Admin only: organization to target. Ignored for managers. Omit for all orgs.")]
        Guid? organizationId = null,
        CancellationToken ct = default)
    {
        var org = Scope(ctx, organizationId);
        SubmissionDetailsDto? details;
        switch (type?.Trim().ToLowerInvariant())
        {
            case "problem":
                details = await repo.GetProblemDetailsAsync(
                    id, org is Guid po ? await repo.GetProblemBoxIdsAsync(po, ct) : null, ct);
                break;
            case "suggestion":
                details = await repo.GetSuggestionDetailsAsync(
                    id, org is Guid so ? await repo.GetSuggestionBoxIdsAsync(so, ct) : null, ct);
                break;
            default:
                return Serialize(new { error = "nepoznat tip; očekivano 'problem' ili 'suggestion'" });
        }

        // Not found OR outside the caller's org scope — same opaque answer either way (no leak of existence).
        return details is null
            ? Serialize(new { error = "nije pronađeno ili van dozvoljenog opsega" })
            : Serialize(details);
    }

    [McpServerTool(Name = "get_global_stats"), Description("Cross-organization aggregate statistics (admin only).")]
    public static async Task<string> GetGlobalStats(
        IReadRepository repo, IInvocationContextAccessor ctx, CancellationToken ct = default)
    {
        // Policy already restricts this tool to admins; no org-scope applies.
        return Serialize(await repo.GetGlobalStatsAsync(ct));
    }
}
