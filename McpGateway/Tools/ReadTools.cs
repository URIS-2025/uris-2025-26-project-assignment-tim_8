using System.ComponentModel;
using ModelContextProtocol.Server;

namespace McpGateway.Tools;

/// <summary>
/// Read tools (analytics + retrieval). Faza A: these are STUBS — the gateway authorizes and audits
/// every call, but the bodies are placeholders. Real data access (scrubbed, org-scoped SQL views)
/// lands in Faza B. Tool names are set explicitly to match the policy keys in appsettings.
/// </summary>
[McpServerToolType]
public static class ReadTools
{
    private const string Todo = "TODO (Faza B): tool not yet implemented";

    [McpServerTool(Name = "get_org_overview"), Description("Aggregate counts of problems/suggestions/boxes for an organization.")]
    public static string GetOrgOverview() => Todo;

    [McpServerTool(Name = "get_problem_stats"), Description("Problem counts by status/category/time for an organization.")]
    public static string GetProblemStats() => Todo;

    [McpServerTool(Name = "get_suggestion_stats"), Description("Suggestion counts by status plus vote totals.")]
    public static string GetSuggestionStats() => Todo;

    [McpServerTool(Name = "list_boxes"), Description("List an organization's boxes (name/status/hasPassword; never the password).")]
    public static string ListBoxes() => Todo;

    [McpServerTool(Name = "search_submissions"), Description("Search problems/suggestions by keyword (scrubbed, no submitter identity).")]
    public static string SearchSubmissions() => Todo;

    [McpServerTool(Name = "get_submission_details"), Description("Full details of one problem/suggestion plus its comments (scrubbed).")]
    public static string GetSubmissionDetails() => Todo;

    [McpServerTool(Name = "get_global_stats"), Description("Cross-organization aggregate statistics (admin only).")]
    public static string GetGlobalStats() => Todo;
}
