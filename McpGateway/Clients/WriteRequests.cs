namespace McpGateway.Clients;

// Request bodies for the write REST endpoints. Each record's property names match the target
// service's DTO exactly; they are serialized with JsonSerializerDefaults.Web (camelCase), which the
// controllers bind case-insensitively. Enum-valued fields (Status/Priority) are plain INTs — no
// service registers a JsonStringEnumConverter, so the wire format is numeric
// (Active=0/Inactive=1; Priority Low=0/Medium=1/High=2).

// ── Box (ProblemBox / SuggestionBox) ─────────────────────────────────────────────────────────────
public record BoxStatusRequest(int Status);                         // PUT .../{id}/status  (BoxStatusUpdateDTO)
public record BoxPasswordRequest(string Password);                  // PUT .../{id}/password (BoxPasswordDTO)

/// <summary>POST /api/{ProblemBox,SuggestionBox} — OrganizationId is FORCED by the gateway.</summary>
public record CreateBoxRequest(
    string Name, string Description, bool IsDarkTheme, string? Password, string CreatedBy, Guid OrganizationId);

// ── Submission update (read-modify-write: full DTO, only Status changed) ──────────────────────────
public record ProblemUpdateRequest(
    Guid Id, string Title, string Description, Guid ProblemBoxId, int Status, int Priority);

// CategoryIds is intentionally omitted (SuggestionRepository.Update ignores it); a partial DTO would
// otherwise wipe Title/Description, so the tool sends the current values back unchanged.
public record SuggestionUpdateRequest(Guid Id, string Title, string Description, int Status);

// ── Comment creation (author = OBO user; IsAnonymous is always false for a manager/admin reply) ───
public record ProblemCommentRequest(
    string CommentText, bool IsAnonymous, Guid ProblemId, Guid ProblemCommentAuthorId);

public record SuggestionCommentRequest(
    string Text, bool IsAnonymous, Guid SuggestionId, Guid CommentAuthorId, string CreatedBy);
