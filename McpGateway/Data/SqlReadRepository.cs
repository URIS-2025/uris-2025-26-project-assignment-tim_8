using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace McpGateway.Data;

/// <summary>
/// The four connection strings for the SELECT-only <c>mcp_read</c> login, one per database.
/// Read from the <c>ReadDb</c> config section. Injected into <see cref="SqlReadRepository"/>.
/// </summary>
public sealed class ReadDbOptions
{
    public string ProblemDb { get; init; } = "";
    public string SuggestionDb { get; init; } = "";
    public string ProblemBoxDb { get; init; } = "";
    public string SuggestionBoxDb { get; init; } = "";
}

/// <summary>
/// Dapper + Microsoft.Data.SqlClient implementation of <see cref="IReadRepository"/>.
///
/// SECURITY: every query below reads ONLY the scrubbed, PII-free views created in
/// <c>McpGateway/Sql/01_read_views.sql</c>, over the SELECT-only <c>mcp_read</c> login
/// (see <c>02_read_login.sql</c>). No PII column is ever selected or mapped. All parameters
/// are bound (never string-concatenated), so the read path is SQL-injection safe.
/// </summary>
public sealed class SqlReadRepository : IReadRepository
{
    private readonly ReadDbOptions _cs;

    public SqlReadRepository(ReadDbOptions connectionStrings)
    {
        _cs = connectionStrings;
    }

    // A fresh connection is opened (and disposed) per query — pooling handles reuse.
    private static async Task<SqlConnection> OpenAsync(string connectionString, CancellationToken ct)
    {
        var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }

    // ── org → box ids ────────────────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Guid>> GetProblemBoxIdsAsync(Guid organizationId, CancellationToken ct)
    {
        const string sql = "SELECT Id FROM dbo.vw_ProblemBox WHERE OrganizationId = @organizationId;";
        await using var conn = await OpenAsync(_cs.ProblemBoxDb, ct);
        var rows = await conn.QueryAsync<Guid>(
            new CommandDefinition(sql, new { organizationId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<Guid>> GetSuggestionBoxIdsAsync(Guid organizationId, CancellationToken ct)
    {
        const string sql = "SELECT Id FROM dbo.vw_SuggestionBox WHERE OrganizationId = @organizationId;";
        await using var conn = await OpenAsync(_cs.SuggestionBoxDb, ct);
        var rows = await conn.QueryAsync<Guid>(
            new CommandDefinition(sql, new { organizationId }, cancellationToken: ct));
        return rows.AsList();
    }

    // ── boxes (organizationId null = all orgs) ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<BoxDto>> ListBoxesAsync(Guid? organizationId, CancellationToken ct)
    {
        // "problem" boxes
        const string problemSql = @"
            SELECT Id, Name, Status, HasPassword
            FROM dbo.vw_ProblemBox
            WHERE (@organizationId IS NULL OR OrganizationId = @organizationId);";

        const string suggestionSql = @"
            SELECT Id, Name, Status, HasPassword
            FROM dbo.vw_SuggestionBox
            WHERE (@organizationId IS NULL OR OrganizationId = @organizationId);";

        var result = new List<BoxDto>();

        await using (var conn = await OpenAsync(_cs.ProblemBoxDb, ct))
        {
            var rows = await conn.QueryAsync<(Guid Id, string Name, int Status, bool HasPassword)>(
                new CommandDefinition(problemSql, new { organizationId }, cancellationToken: ct));
            result.AddRange(rows.Select(r => new BoxDto(r.Id, "problem", r.Name, r.Status, r.HasPassword)));
        }

        await using (var conn = await OpenAsync(_cs.SuggestionBoxDb, ct))
        {
            var rows = await conn.QueryAsync<(Guid Id, string Name, int Status, bool HasPassword)>(
                new CommandDefinition(suggestionSql, new { organizationId }, cancellationToken: ct));
            result.AddRange(rows.Select(r => new BoxDto(r.Id, "suggestion", r.Name, r.Status, r.HasPassword)));
        }

        return result;
    }

    public async Task<int> CountProblemBoxesAsync(Guid? organizationId, CancellationToken ct)
    {
        const string sql = @"
            SELECT COUNT(*) FROM dbo.vw_ProblemBox
            WHERE (@organizationId IS NULL OR OrganizationId = @organizationId);";
        await using var conn = await OpenAsync(_cs.ProblemBoxDb, ct);
        return await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { organizationId }, cancellationToken: ct));
    }

    public async Task<int> CountSuggestionBoxesAsync(Guid? organizationId, CancellationToken ct)
    {
        const string sql = @"
            SELECT COUNT(*) FROM dbo.vw_SuggestionBox
            WHERE (@organizationId IS NULL OR OrganizationId = @organizationId);";
        await using var conn = await OpenAsync(_cs.SuggestionBoxDb, ct);
        return await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { organizationId }, cancellationToken: ct));
    }

    // ── submissions (scoped by resolved boxIds) ────────────────────────────────────────────────────
    //
    // boxIds contract:
    //   null       => admin/global — no box filter (count/read everything)
    //   EMPTY list => a scoped caller with zero boxes — MUST return 0 immediately.
    //                 We never run an `IN ()` (that would be a syntax/semantic error and
    //                 must never silently mean "all").

    public async Task<int> CountProblemsAsync(IReadOnlyList<Guid>? problemBoxIds, CancellationToken ct)
    {
        if (problemBoxIds is { Count: 0 }) return 0; // empty scope => zero rows

        var (sql, param) = ScopedCountSql("dbo.vw_Problem", "ProblemBoxId", problemBoxIds);
        await using var conn = await OpenAsync(_cs.ProblemDb, ct);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, param, cancellationToken: ct));
    }

    public async Task<int> CountSuggestionsAsync(IReadOnlyList<Guid>? suggestionBoxIds, CancellationToken ct)
    {
        if (suggestionBoxIds is { Count: 0 }) return 0;

        var (sql, param) = ScopedCountSql("dbo.vw_Suggestion", "SuggestionBoxId", suggestionBoxIds);
        await using var conn = await OpenAsync(_cs.SuggestionDb, ct);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, param, cancellationToken: ct));
    }

    private static (string sql, object? param) ScopedCountSql(
        string view, string boxIdColumn, IReadOnlyList<Guid>? boxIds)
    {
        if (boxIds is null)
            return ($"SELECT COUNT(*) FROM {view};", null);

        // Dapper expands the list parameter into (@boxIds1, @boxIds2, ...).
        return ($"SELECT COUNT(*) FROM {view} WHERE {boxIdColumn} IN @boxIds;", new { boxIds });
    }

    // ── stats ──────────────────────────────────────────────────────────────────────────────────────

    public async Task<ProblemStatsDto> GetProblemStatsAsync(IReadOnlyList<Guid>? problemBoxIds, CancellationToken ct)
    {
        if (problemBoxIds is { Count: 0 })
            return new ProblemStatsDto(0, Array.Empty<CodeCount>(), Array.Empty<CodeCount>());

        var scope = problemBoxIds is null ? "" : " WHERE ProblemBoxId IN @boxIds";
        object? param = problemBoxIds is null ? null : new { boxIds = problemBoxIds };

        string statusSql   = $"SELECT Status AS Code, COUNT(*) AS Count FROM dbo.vw_Problem{scope} GROUP BY Status;";
        string prioritySql = $"SELECT Priority AS Code, COUNT(*) AS Count FROM dbo.vw_Problem{scope} GROUP BY Priority;";
        string totalSql    = $"SELECT COUNT(*) FROM dbo.vw_Problem{scope};";

        await using var conn = await OpenAsync(_cs.ProblemDb, ct);

        var byStatus   = (await conn.QueryAsync<CodeCount>(new CommandDefinition(statusSql, param, cancellationToken: ct))).AsList();
        var byPriority = (await conn.QueryAsync<CodeCount>(new CommandDefinition(prioritySql, param, cancellationToken: ct))).AsList();
        var total      = await conn.ExecuteScalarAsync<int>(new CommandDefinition(totalSql, param, cancellationToken: ct));

        return new ProblemStatsDto(total, byStatus, byPriority);
    }

    public async Task<SuggestionStatsDto> GetSuggestionStatsAsync(IReadOnlyList<Guid>? suggestionBoxIds, CancellationToken ct)
    {
        if (suggestionBoxIds is { Count: 0 })
            return new SuggestionStatsDto(0, 0, Array.Empty<CodeCount>());

        var scope = suggestionBoxIds is null ? "" : " WHERE SuggestionBoxId IN @boxIds";
        object? param = suggestionBoxIds is null ? null : new { boxIds = suggestionBoxIds };

        string statusSql = $"SELECT Status AS Code, COUNT(*) AS Count FROM dbo.vw_Suggestion{scope} GROUP BY Status;";
        string totalSql  = $"SELECT COUNT(*) FROM dbo.vw_Suggestion{scope};";
        // TotalVotes: votes whose suggestion is within the box scope (join scope on the suggestion view).
        string votesSql = suggestionBoxIds is null
            ? "SELECT COUNT(*) FROM dbo.vw_Vote;"
            : @"SELECT COUNT(*)
                FROM dbo.vw_Vote v
                INNER JOIN dbo.vw_Suggestion s ON s.Id = v.SuggestionId
                WHERE s.SuggestionBoxId IN @boxIds;";

        await using var conn = await OpenAsync(_cs.SuggestionDb, ct);

        var byStatus   = (await conn.QueryAsync<CodeCount>(new CommandDefinition(statusSql, param, cancellationToken: ct))).AsList();
        var total      = await conn.ExecuteScalarAsync<int>(new CommandDefinition(totalSql, param, cancellationToken: ct));
        var totalVotes = await conn.ExecuteScalarAsync<int>(new CommandDefinition(votesSql, param, cancellationToken: ct));

        return new SuggestionStatsDto(total, totalVotes, byStatus);
    }

    // ── search ───────────────────────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SubmissionSummaryDto>> SearchProblemsAsync(
        string keyword, IReadOnlyList<Guid>? problemBoxIds, int cap, CancellationToken ct)
    {
        if (problemBoxIds is { Count: 0 }) return Array.Empty<SubmissionSummaryDto>();

        var scope = problemBoxIds is null ? "" : " AND ProblemBoxId IN @boxIds";
        // Keyword is passed as a PARAMETER VALUE ("%kw%") — never concatenated into the SQL text.
        string sql = $@"
            SELECT TOP (@cap) Id, Title, Status, CreatedAt
            FROM dbo.vw_Problem
            WHERE (Title LIKE @pattern OR Description LIKE @pattern){scope}
            ORDER BY CreatedAt DESC;";

        var param = new
        {
            cap,
            pattern = "%" + keyword + "%",
            boxIds = problemBoxIds
        };

        await using var conn = await OpenAsync(_cs.ProblemDb, ct);
        var rows = await conn.QueryAsync<(Guid Id, string Title, int Status, DateTime CreatedAt)>(
            new CommandDefinition(sql, param, cancellationToken: ct));
        return rows.Select(r => new SubmissionSummaryDto(r.Id, "problem", r.Title, r.Status, r.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<SubmissionSummaryDto>> SearchSuggestionsAsync(
        string keyword, IReadOnlyList<Guid>? suggestionBoxIds, int cap, CancellationToken ct)
    {
        if (suggestionBoxIds is { Count: 0 }) return Array.Empty<SubmissionSummaryDto>();

        var scope = suggestionBoxIds is null ? "" : " AND SuggestionBoxId IN @boxIds";
        string sql = $@"
            SELECT TOP (@cap) Id, Title, Status, CreatedAt
            FROM dbo.vw_Suggestion
            WHERE (Title LIKE @pattern OR Description LIKE @pattern){scope}
            ORDER BY CreatedAt DESC;";

        var param = new
        {
            cap,
            pattern = "%" + keyword + "%",
            boxIds = suggestionBoxIds
        };

        await using var conn = await OpenAsync(_cs.SuggestionDb, ct);
        var rows = await conn.QueryAsync<(Guid Id, string Title, int Status, DateTime CreatedAt)>(
            new CommandDefinition(sql, param, cancellationToken: ct));
        return rows.Select(r => new SubmissionSummaryDto(r.Id, "suggestion", r.Title, r.Status, r.CreatedAt)).ToList();
    }

    // ── details (scope enforced) ───────────────────────────────────────────────────────────────────

    // Head row for a submission. A reference type (not a value tuple) so QuerySingleOrDefaultAsync
    // returns null on not-found — no Guid.Empty sentinel (which would collide with a real zero-GUID
    // key) — and so a NULL column would surface as a null field rather than being silently mapped.
    // The views (01_read_views.sql) project the underlying NOT NULL columns, so Title/Description
    // are always present in practice.
    private sealed record SubmissionHeadRow(Guid Id, string Title, string Description, int Status, DateTime CreatedAt);

    public async Task<SubmissionDetailsDto?> GetProblemDetailsAsync(
        Guid id, IReadOnlyList<Guid>? allowedProblemBoxIds, CancellationToken ct)
    {
        // Empty allowed scope => caller may see nothing.
        if (allowedProblemBoxIds is { Count: 0 }) return null;

        var scope = allowedProblemBoxIds is null ? "" : " AND ProblemBoxId IN @boxIds";
        string headSql = $@"
            SELECT Id, Title, Description, Status, CreatedAt
            FROM dbo.vw_Problem
            WHERE Id = @id{scope};";

        await using var conn = await OpenAsync(_cs.ProblemDb, ct);

        var head = await conn.QuerySingleOrDefaultAsync<SubmissionHeadRow>(
            new CommandDefinition(headSql, new { id, boxIds = allowedProblemBoxIds }, cancellationToken: ct));
        if (head is null) return null; // not found OR out of scope — same opaque answer

        const string commentsSql = @"
            SELECT CommentText AS Text, IsAnonymous, CreatedAt
            FROM dbo.vw_ProblemComment
            WHERE ProblemId = @id;";
        var comments = (await conn.QueryAsync<CommentDto>(
            new CommandDefinition(commentsSql, new { id }, cancellationToken: ct))).AsList();

        return new SubmissionDetailsDto(
            head.Id, "problem", head.Title, head.Description, head.Status, head.CreatedAt, comments);
    }

    public async Task<SubmissionDetailsDto?> GetSuggestionDetailsAsync(
        Guid id, IReadOnlyList<Guid>? allowedSuggestionBoxIds, CancellationToken ct)
    {
        if (allowedSuggestionBoxIds is { Count: 0 }) return null;

        var scope = allowedSuggestionBoxIds is null ? "" : " AND SuggestionBoxId IN @boxIds";
        string headSql = $@"
            SELECT Id, Title, Description, Status, CreatedAt
            FROM dbo.vw_Suggestion
            WHERE Id = @id{scope};";

        await using var conn = await OpenAsync(_cs.SuggestionDb, ct);

        var head = await conn.QuerySingleOrDefaultAsync<SubmissionHeadRow>(
            new CommandDefinition(headSql, new { id, boxIds = allowedSuggestionBoxIds }, cancellationToken: ct));
        if (head is null) return null; // not found OR out of scope — same opaque answer

        const string commentsSql = @"
            SELECT Text, IsAnonymous, CreatedAt
            FROM dbo.vw_SuggestionComment
            WHERE SuggestionId = @id;";
        var comments = (await conn.QueryAsync<CommentDto>(
            new CommandDefinition(commentsSql, new { id }, cancellationToken: ct))).AsList();

        return new SubmissionDetailsDto(
            head.Id, "suggestion", head.Title, head.Description, head.Status, head.CreatedAt, comments);
    }

    // ── read-modify-write sources (scope enforced; null = not found OR out of scope) ─────────────────

    public async Task<ProblemUpdateFieldsDto?> GetProblemUpdateFieldsAsync(
        Guid id, IReadOnlyList<Guid>? allowedProblemBoxIds, CancellationToken ct)
    {
        if (allowedProblemBoxIds is { Count: 0 }) return null; // scoped caller with no boxes → deny

        var scope = allowedProblemBoxIds is null ? "" : " AND ProblemBoxId IN @boxIds";
        string sql = $@"
            SELECT Title, Description, ProblemBoxId, Priority, Status
            FROM dbo.vw_Problem
            WHERE Id = @id{scope};";

        await using var conn = await OpenAsync(_cs.ProblemDb, ct);
        return await conn.QuerySingleOrDefaultAsync<ProblemUpdateFieldsDto>(
            new CommandDefinition(sql, new { id, boxIds = allowedProblemBoxIds }, cancellationToken: ct));
    }

    public async Task<SuggestionUpdateFieldsDto?> GetSuggestionUpdateFieldsAsync(
        Guid id, IReadOnlyList<Guid>? allowedSuggestionBoxIds, CancellationToken ct)
    {
        if (allowedSuggestionBoxIds is { Count: 0 }) return null;

        var scope = allowedSuggestionBoxIds is null ? "" : " AND SuggestionBoxId IN @boxIds";
        // vw_Suggestion normalizes the string-stored Status back to its numeric code (see 01_read_views.sql).
        string sql = $@"
            SELECT Title, Description, Status
            FROM dbo.vw_Suggestion
            WHERE Id = @id{scope};";

        await using var conn = await OpenAsync(_cs.SuggestionDb, ct);
        return await conn.QuerySingleOrDefaultAsync<SuggestionUpdateFieldsDto>(
            new CommandDefinition(sql, new { id, boxIds = allowedSuggestionBoxIds }, cancellationToken: ct));
    }

    // ── global (admin only) ────────────────────────────────────────────────────────────────────────

    public async Task<GlobalStatsDto> GetGlobalStatsAsync(CancellationToken ct)
    {
        // Boxes / problems / suggestions are simple totals across all rows.
        int problemBoxes, suggestionBoxes, problems, suggestions;
        List<Guid> problemBoxOrgIds, suggestionBoxOrgIds;

        await using (var conn = await OpenAsync(_cs.ProblemBoxDb, ct))
        {
            problemBoxes = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT COUNT(*) FROM dbo.vw_ProblemBox;", cancellationToken: ct));
            problemBoxOrgIds = (await conn.QueryAsync<Guid>(
                new CommandDefinition("SELECT DISTINCT OrganizationId FROM dbo.vw_ProblemBox;", cancellationToken: ct))).AsList();
        }

        await using (var conn = await OpenAsync(_cs.SuggestionBoxDb, ct))
        {
            suggestionBoxes = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT COUNT(*) FROM dbo.vw_SuggestionBox;", cancellationToken: ct));
            suggestionBoxOrgIds = (await conn.QueryAsync<Guid>(
                new CommandDefinition("SELECT DISTINCT OrganizationId FROM dbo.vw_SuggestionBox;", cancellationToken: ct))).AsList();
        }

        await using (var conn = await OpenAsync(_cs.ProblemDb, ct))
        {
            problems = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT COUNT(*) FROM dbo.vw_Problem;", cancellationToken: ct));
        }

        await using (var conn = await OpenAsync(_cs.SuggestionDb, ct))
        {
            suggestions = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT COUNT(*) FROM dbo.vw_Suggestion;", cancellationToken: ct));
        }

        // Organizations = distinct org ids across BOTH box databases (union across DBs in memory,
        // since the databases are separate and cannot be cross-DB joined by the read login).
        var orgs = new HashSet<Guid>(problemBoxOrgIds);
        orgs.UnionWith(suggestionBoxOrgIds);

        return new GlobalStatsDto(orgs.Count, problems, suggestions, problemBoxes + suggestionBoxes);
    }
}
