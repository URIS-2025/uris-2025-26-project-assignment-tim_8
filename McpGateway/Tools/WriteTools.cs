using System.ComponentModel;
using System.Text.Json;
using McpGateway.Authorization;
using McpGateway.Clients;
using McpGateway.Data;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpGateway.Tools;

/// <summary>
/// Write tools (state-changing actions). Each one: resolves the caller's org via
/// <see cref="ToolScope"/>, VERIFIES org ownership of the target BEFORE calling anything (the REST
/// endpoints have no <c>[Authorize]</c>, so the gateway is the only guard), then forwards the call to
/// the existing REST endpoint carrying the caller's on-behalf-of bearer, and maps the outcome.
///
/// Errors (denied scope, bad input, or a non-2xx REST response) are returned as a
/// <see cref="CallToolResult"/> with <c>IsError = true</c> — never thrown, never swallowed. Raw 5xx
/// bodies are never surfaced to the agent. Tool names match the policy keys in appsettings.
/// </summary>
[McpServerToolType]
public static class WriteTools
{
    private const string Todo = "TODO (Faza B-write): not yet implemented";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [McpServerTool(Name = "set_box_status"), Description("Activate or deactivate a box. Args: type ('problem'|'suggestion'), id, status (0=Active, 1=Inactive).")]
    public static async Task<CallToolResult> SetBoxStatus(
        ProblemBoxServiceClient problemBoxes, SuggestionBoxServiceClient suggestionBoxes,
        IReadRepository repo, IInvocationContextAccessor ctx,
        [Description("'problem' or 'suggestion'.")] string type,
        [Description("The box id.")] Guid id,
        [Description("New status: 0=Active, 1=Inactive.")] int status,
        CancellationToken ct = default)
    {
        var t = Normalize(type);
        if (t is null) return UnknownType();

        // Fail-closed: throws if the authorization filter did not set the context. Manager → own org;
        // admin → null (no org lock). The LLM cannot widen scope — org never comes from tool args.
        var org = ToolScope.Resolve(ctx.Current, requestedOrganizationId: null);

        // Org-ownership guard BEFORE the write: the REST endpoint has no [Authorize], so this is the
        // only barrier. Admin (org == null) may target any box; a manager is limited to their boxes.
        if (!await BoxInScopeAsync(repo, t, id, org, ct)) return Deny();

        var bearer = ctx.Current!.BearerToken;
        var result = t == ProblemType
            ? await problemBoxes.SetStatusAsync(id, status, bearer, ct)
            : await suggestionBoxes.SetStatusAsync(id, status, bearer, ct);
        return Map(result);
    }

    [McpServerTool(Name = "set_box_password"), Description("Set (or clear) a box password. Args: type ('problem'|'suggestion'), id, password.")]
    public static async Task<CallToolResult> SetBoxPassword(
        ProblemBoxServiceClient problemBoxes, SuggestionBoxServiceClient suggestionBoxes,
        IReadRepository repo, IInvocationContextAccessor ctx,
        [Description("'problem' or 'suggestion'.")] string type,
        [Description("The box id.")] Guid id,
        [Description("The new password (sent, never logged).")] string password,
        CancellationToken ct = default)
    {
        var t = Normalize(type);
        if (t is null) return UnknownType();

        var org = ToolScope.Resolve(ctx.Current, requestedOrganizationId: null);
        if (!await BoxInScopeAsync(repo, t, id, org, ct)) return Deny();

        var bearer = ctx.Current!.BearerToken;
        var result = t == ProblemType
            ? await problemBoxes.SetPasswordAsync(id, password, bearer, ct)
            : await suggestionBoxes.SetPasswordAsync(id, password, bearer, ct);
        return Map(result);
    }

    [McpServerTool(Name = "create_box"), Description("Create a new problem or suggestion box. Args: type, name, description, isDarkTheme?, password?, organizationId? (admin only).")]
    public static async Task<CallToolResult> CreateBox(
        ProblemBoxServiceClient problemBoxes, SuggestionBoxServiceClient suggestionBoxes,
        IInvocationContextAccessor ctx,
        [Description("'problem' or 'suggestion'.")] string type,
        [Description("Box name.")] string name,
        [Description("Box description.")] string description,
        [Description("Dark theme flag (default false).")] bool isDarkTheme = false,
        [Description("Optional box password.")] string? password = null,
        [Description("Admin only: the org to create the box in. Ignored for managers (own org).")]
        Guid? organizationId = null,
        CancellationToken ct = default)
    {
        var t = Normalize(type);
        if (t is null) return UnknownType();

        // Force-org: a manager is pinned to their OWN org (the LLM-supplied organizationId is ignored,
        // so a box can never be created in another org); an admin MUST name the target org explicitly.
        var org = ToolScope.Resolve(ctx.Current, requestedOrganizationId: organizationId);
        if (org is not Guid effectiveOrg)
            return Error("organizationId je obavezan za administratora");

        var box = new CreateBoxRequest(name, description, isDarkTheme, password, ctx.Current!.UserId, effectiveOrg);
        var bearer = ctx.Current.BearerToken;
        var result = t == ProblemType
            ? await problemBoxes.CreateAsync(box, bearer, ct)
            : await suggestionBoxes.CreateAsync(box, bearer, ct);
        return Map(result);
    }

    [McpServerTool(Name = "add_comment"), Description("Post a comment (reply) on a problem or suggestion. Args: type ('problem'|'suggestion'), id, text.")]
    public static async Task<CallToolResult> AddComment(
        ProblemServiceClient problems, SuggestionServiceClient suggestions,
        IReadRepository repo, IInvocationContextAccessor ctx,
        [Description("'problem' or 'suggestion'.")] string type,
        [Description("The submission id.")] Guid id,
        [Description("The comment text.")] string text,
        CancellationToken ct = default)
    {
        var t = Normalize(type);
        if (t is null) return UnknownType();

        var org = ToolScope.Resolve(ctx.Current, requestedOrganizationId: null);
        if (!await SubmissionInScopeAsync(repo, t, id, org, ct)) return Deny();

        // Author is the OBO user; a manager/admin reply is never anonymous.
        if (!Guid.TryParse(ctx.Current!.UserId, out var authorId))
            return Error("nevažeći identitet korisnika (UserId nije GUID)");

        var bearer = ctx.Current.BearerToken;
        var result = t == ProblemType
            ? await problems.AddCommentAsync(
                new ProblemCommentRequest(text, IsAnonymous: false, ProblemId: id, ProblemCommentAuthorId: authorId),
                bearer, ct)
            : await suggestions.AddCommentAsync(
                new SuggestionCommentRequest(text, IsAnonymous: false, SuggestionId: id,
                    CommentAuthorId: authorId, CreatedBy: ctx.Current.UserId),
                bearer, ct);
        return Map(result);
    }

    [McpServerTool(Name = "update_submission_status"), Description("Change the status of a problem or suggestion. Args: type ('problem'|'suggestion'), id, status (0=Active, 1=Inactive).")]
    public static async Task<CallToolResult> UpdateSubmissionStatus(
        ProblemServiceClient problems, SuggestionServiceClient suggestions,
        IReadRepository repo, IInvocationContextAccessor ctx,
        [Description("'problem' or 'suggestion'.")] string type,
        [Description("The submission id.")] Guid id,
        [Description("New status: 0=Active, 1=Inactive.")] int status,
        CancellationToken ct = default)
    {
        var t = Normalize(type);
        if (t is null) return UnknownType();

        var org = ToolScope.Resolve(ctx.Current, requestedOrganizationId: null);
        var bearer = ctx.Current!.BearerToken;

        // Read-modify-write: the REST PUT takes a FULL DTO and overwrites unset fields, so the current
        // values are read first (that read also enforces org scope: null = not found / out of scope).
        // Only Status is changed; everything else is echoed back unchanged.
        if (t == ProblemType)
        {
            var boxIds = org is Guid o ? await repo.GetProblemBoxIdsAsync(o, ct) : null;
            var cur = await repo.GetProblemUpdateFieldsAsync(id, boxIds, ct);
            if (cur is null) return Deny();

            var result = await problems.UpdateAsync(
                new ProblemUpdateRequest(id, cur.Title, cur.Description, cur.ProblemBoxId, status, cur.Priority),
                bearer, ct);
            return Map(result);
        }
        else
        {
            var boxIds = org is Guid o ? await repo.GetSuggestionBoxIdsAsync(o, ct) : null;
            var cur = await repo.GetSuggestionUpdateFieldsAsync(id, boxIds, ct);
            if (cur is null) return Deny();

            // CategoryIds intentionally omitted (repo ignores it); Title/Description carried unchanged.
            var result = await suggestions.UpdateAsync(
                new SuggestionUpdateRequest(id, cur.Title, cur.Description, status), bearer, ct);
            return Map(result);
        }
    }

    // ── shared helpers ─────────────────────────────────────────────────────────────────────────────

    private const string ProblemType = "problem";
    private const string SuggestionType = "suggestion";

    /// <summary>Canonicalizes the type argument to "problem"/"suggestion", or null if unrecognized.</summary>
    private static string? Normalize(string? type) => type?.Trim().ToLowerInvariant() switch
    {
        ProblemType => ProblemType,
        SuggestionType => SuggestionType,
        _ => null,
    };

    /// <summary>
    /// True if the box may be acted on by this caller: admin (<paramref name="org"/> == null) may
    /// target any box; a manager is limited to the boxes their org owns.
    /// </summary>
    private static async Task<bool> BoxInScopeAsync(
        IReadRepository repo, string type, Guid boxId, Guid? org, CancellationToken ct)
    {
        if (org is not Guid o) return true; // admin / global — any box allowed
        var boxIds = type == ProblemType
            ? await repo.GetProblemBoxIdsAsync(o, ct)
            : await repo.GetSuggestionBoxIdsAsync(o, ct);
        return boxIds.Contains(boxId);
    }

    /// <summary>
    /// True if the submission may be acted on by this caller. Admin (<paramref name="org"/> == null)
    /// may target any submission; for a manager it reuses the RMW-source read (non-null iff the
    /// submission exists AND lies within the org's boxes), so the scope check leaks no existence.
    /// </summary>
    private static async Task<bool> SubmissionInScopeAsync(
        IReadRepository repo, string type, Guid id, Guid? org, CancellationToken ct)
    {
        if (org is not Guid o) return true; // admin / global — any submission
        if (type == ProblemType)
        {
            var boxIds = await repo.GetProblemBoxIdsAsync(o, ct);
            return await repo.GetProblemUpdateFieldsAsync(id, boxIds, ct) is not null;
        }
        else
        {
            var boxIds = await repo.GetSuggestionBoxIdsAsync(o, ct);
            return await repo.GetSuggestionUpdateFieldsAsync(id, boxIds, ct) is not null;
        }
    }

    private static CallToolResult Text(string text, bool isError = false) =>
        new() { IsError = isError, Content = [new TextContentBlock { Text = text }] };

    private static CallToolResult Ok(object payload) => Text(JsonSerializer.Serialize(payload, Json));

    private static CallToolResult Error(string message) =>
        Text(JsonSerializer.Serialize(new { error = message }, Json), isError: true);

    private static CallToolResult UnknownType() =>
        Error("nepoznat tip; očekivano 'problem' ili 'suggestion'");

    /// <summary>Opaque refusal — identical for "not found" and "out of scope" so existence never leaks.</summary>
    private static CallToolResult Deny() =>
        Error("zabranjeno: cilj ne postoji ili je van vašeg opsega");

    /// <summary>
    /// Maps a REST outcome to a tool result. 2xx → success. A 409 conflict is surfaced (it carries the
    /// inherited T2/T3 business rule — inactive box / password gate); other 4xx report the status
    /// only; a 5xx or transport failure gets a generic message (a raw server body never reaches the LLM).
    /// </summary>
    private static CallToolResult Map(WriteResult r)
    {
        if (r.Success) return Ok(new { success = true, status = r.StatusCode });

        return r.StatusCode switch
        {
            409 => Error("odbijeno: sukob stanja (npr. kutija je neaktivna ili je lozinka već postavljena)"),
            >= 400 and < 500 => Error($"odbijeno (status {r.StatusCode})"),
            0 => Error("ciljni servis nije dostupan ili nije odgovorio na vreme"),
            _ => Error("greška na ciljnom servisu"),
        };
    }
}
