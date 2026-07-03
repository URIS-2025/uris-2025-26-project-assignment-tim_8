namespace McpGateway.Data;

// Scrubbed read DTOs returned to the agent. By construction NONE of these carry PII: no
// AnonymousUserId, no *AuthorId, no CreatedBy, no password/hash/token. Status/Priority are exposed
// as their numeric codes (no coupling to other services' enum definitions); a friendly label map
// is a later nice-to-have. "Type" is "problem" | "suggestion".

public record OrgOverviewDto(int Problems, int Suggestions, int Boxes);

/// <summary>A count grouped by a numeric status/priority code.</summary>
public record CodeCount(int Code, int Count);

public record ProblemStatsDto(int Total, IReadOnlyList<CodeCount> ByStatus, IReadOnlyList<CodeCount> ByPriority);

public record SuggestionStatsDto(int Total, int TotalVotes, IReadOnlyList<CodeCount> ByStatus);

/// <summary>A box, scrubbed: never the password itself — only whether one is set.</summary>
public record BoxDto(Guid Id, string Type, string Name, int Status, bool HasPassword);

public record SubmissionSummaryDto(Guid Id, string Type, string Title, int Status, DateTime CreatedAt);

/// <summary>A comment, scrubbed: text + anonymity flag only, never the author id / CreatedBy.</summary>
public record CommentDto(string Text, bool IsAnonymous, DateTime? CreatedAt);

public record SubmissionDetailsDto(
    Guid Id, string Type, string Title, string Description, int Status, DateTime CreatedAt,
    IReadOnlyList<CommentDto> Comments);

public record GlobalStatsDto(int Organizations, int Problems, int Suggestions, int Boxes);

// ── Read-modify-write source rows (internal to write tools; NOT returned to the agent) ────────────
// The REST update endpoints require a FULL DTO and overwrite unset fields, so a write tool must read
// the current values first. These carry no PII (Title/Description are already exposed by read tools;
// Status/Priority are numeric codes). A null result = not found OR outside the caller's box scope —
// so the same call doubles as the org-ownership check (opaque, no existence leak).
public record ProblemUpdateFieldsDto(string Title, string Description, Guid ProblemBoxId, int Priority, int Status);

public record SuggestionUpdateFieldsDto(string Title, string Description, int Status);
