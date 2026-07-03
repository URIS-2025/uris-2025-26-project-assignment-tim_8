namespace McpGateway.Data;

/// <summary>
/// Read-only data access for the gateway's read tools. Every implementation MUST go through a
/// SELECT-only DB login restricted to scrubbed, PII-free views (see <c>McpGateway/Sql</c>).
///
/// Org-scoping contract (the gateway composes it — the SQL never joins across databases):
/// <list type="bullet">
///   <item>Box methods take an <c>organizationId</c> — box views carry OrganizationId directly.</item>
///   <item>Submission methods take the already-resolved <c>boxIds</c> — a submission view lives in a
///   single DB and is filtered by its box-id foreign key. <c>null</c> boxIds = admin/global (no
///   filter); an <b>empty</b> list = a scoped caller with zero boxes → zero results (never "all").</item>
/// </list>
/// </summary>
public interface IReadRepository
{
    // ── org → box ids (box views carry OrganizationId) ───────────────────────────────────────────
    Task<IReadOnlyList<Guid>> GetProblemBoxIdsAsync(Guid organizationId, CancellationToken ct);
    Task<IReadOnlyList<Guid>> GetSuggestionBoxIdsAsync(Guid organizationId, CancellationToken ct);

    // ── boxes (organizationId null = all orgs) ───────────────────────────────────────────────────
    Task<IReadOnlyList<BoxDto>> ListBoxesAsync(Guid? organizationId, CancellationToken ct);
    Task<int> CountProblemBoxesAsync(Guid? organizationId, CancellationToken ct);
    Task<int> CountSuggestionBoxesAsync(Guid? organizationId, CancellationToken ct);

    // ── submissions (scoped by resolved boxIds; null = all) ──────────────────────────────────────
    Task<int> CountProblemsAsync(IReadOnlyList<Guid>? problemBoxIds, CancellationToken ct);
    Task<int> CountSuggestionsAsync(IReadOnlyList<Guid>? suggestionBoxIds, CancellationToken ct);

    Task<ProblemStatsDto> GetProblemStatsAsync(IReadOnlyList<Guid>? problemBoxIds, CancellationToken ct);
    Task<SuggestionStatsDto> GetSuggestionStatsAsync(IReadOnlyList<Guid>? suggestionBoxIds, CancellationToken ct);

    Task<IReadOnlyList<SubmissionSummaryDto>> SearchProblemsAsync(
        string keyword, IReadOnlyList<Guid>? problemBoxIds, int cap, CancellationToken ct);
    Task<IReadOnlyList<SubmissionSummaryDto>> SearchSuggestionsAsync(
        string keyword, IReadOnlyList<Guid>? suggestionBoxIds, int cap, CancellationToken ct);

    /// <summary>Returns null if the problem does not exist OR is outside the allowed boxes (scope).</summary>
    Task<SubmissionDetailsDto?> GetProblemDetailsAsync(
        Guid id, IReadOnlyList<Guid>? allowedProblemBoxIds, CancellationToken ct);
    Task<SubmissionDetailsDto?> GetSuggestionDetailsAsync(
        Guid id, IReadOnlyList<Guid>? allowedSuggestionBoxIds, CancellationToken ct);

    // ── read-modify-write sources (write tools; null = not found OR out of scope → deny) ─────────
    // Same boxIds contract as the details methods: null = admin/global (no filter); empty = scoped
    // caller with zero boxes → null. Returning null doubles as the org-ownership check for writes.
    Task<ProblemUpdateFieldsDto?> GetProblemUpdateFieldsAsync(
        Guid id, IReadOnlyList<Guid>? allowedProblemBoxIds, CancellationToken ct);
    Task<SuggestionUpdateFieldsDto?> GetSuggestionUpdateFieldsAsync(
        Guid id, IReadOnlyList<Guid>? allowedSuggestionBoxIds, CancellationToken ct);

    // ── global (admin only) ──────────────────────────────────────────────────────────────────────
    Task<GlobalStatsDto> GetGlobalStatsAsync(CancellationToken ct);
}
