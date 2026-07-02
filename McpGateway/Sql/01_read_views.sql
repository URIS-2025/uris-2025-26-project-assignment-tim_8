-- =============================================================================
-- McpGateway — Scrubbed read views (Faza: SQL/Dapper read layer)
-- =============================================================================
-- SECURITY CONTRACT
--   These views are the ONLY database objects the SELECT-only login `mcp_read`
--   (see 02_read_login.sql) is permitted to read. The base tables are DENY'd to
--   that login as defense-in-depth.
--
--   Every view below deliberately OMITS all PII / sensitive columns:
--     * NEVER expose Password (boxes) — only a boolean HasPassword flag.
--     * NEVER expose CreatedBy (SuggestionBox / SuggestionComment).
--     * NEVER expose any author identity:
--         ProblemComment.ProblemCommentAuthorId
--         Suggestion.AnonymousUserId
--         SuggestionComment.CommentAuthorId
--         Vote.VoteAuthorId
--
--   Status / Priority are exposed as their numeric codes so the gateway is not
--   coupled to any other service's enum definition.
--
--   Run each section inside the database named in its header. The four
--   databases are separate SQL Server databases (no cross-DB joins here).
-- =============================================================================


-- =============================================================================
-- ProblemBoxDB
-- Table: ProblemBoxes  (EF DbSet<ProblemBox> ProblemBoxes)
-- PII omitted: Password (exposed only as HasPassword), Description, IsDarkTheme, CreatedAt
-- =============================================================================
USE ProblemBoxDB;
GO

CREATE OR ALTER VIEW dbo.vw_ProblemBox
AS
    SELECT
        Id,
        Name,
        Status,
        OrganizationId,
        -- HasPassword: true when a password is set, without ever revealing it.
        CAST(CASE WHEN Password IS NULL OR Password = '' THEN 0 ELSE 1 END AS bit) AS HasPassword
    FROM dbo.ProblemBoxes;
GO


-- =============================================================================
-- SuggestionBoxDB
-- Table: SuggestionBoxes  (EF DbSet<SuggestionBox> SuggestionBoxes)
-- PII omitted: Password (exposed only as HasPassword), CreatedBy, Description, IsDarkTheme, CreatedAt
-- =============================================================================
USE SuggestionBoxDB;
GO

CREATE OR ALTER VIEW dbo.vw_SuggestionBox
AS
    SELECT
        Id,
        Name,
        Status,
        OrganizationId,
        CAST(CASE WHEN Password IS NULL OR Password = '' THEN 0 ELSE 1 END AS bit) AS HasPassword
    FROM dbo.SuggestionBoxes;
GO


-- =============================================================================
-- ProblemDB
-- Tables: Problems, ProblemComments
-- =============================================================================
USE ProblemDB;
GO

-- Problem: no author identity to strip here (Problem has none). All columns are safe.
CREATE OR ALTER VIEW dbo.vw_Problem
AS
    SELECT
        Id,
        Title,
        Description,
        Status,
        Priority,
        ProblemBoxId,
        CreatedAt
    FROM dbo.Problems;
GO

-- ProblemComment: NEVER expose ProblemCommentAuthorId.
-- NOTE: the ProblemComment entity has NO CreatedAt column, so we project a typed
--       NULL to satisfy the CommentDto.CreatedAt (DateTime?) shape.
CREATE OR ALTER VIEW dbo.vw_ProblemComment
AS
    SELECT
        Id,
        ProblemId,
        CommentText,
        IsAnonymous,
        CAST(NULL AS datetime2) AS CreatedAt
    FROM dbo.ProblemComments;
GO


-- =============================================================================
-- SuggestionDB
-- Tables: Suggestions, SuggestionComments, Votes
-- NOTE: Suggestion.Status is stored as a STRING (EF HasConversion<string>()),
--       so it is normalized back to the numeric code the DTOs expect.
-- =============================================================================
USE SuggestionDB;
GO

-- Suggestion: NEVER expose AnonymousUserId. Status string -> numeric code.
CREATE OR ALTER VIEW dbo.vw_Suggestion
AS
    SELECT
        Id,
        Title,
        Description,
        CAST(
            CASE Status
                WHEN 'Active'   THEN 0
                WHEN 'Inactive' THEN 1
                ELSE -1
            END AS int) AS Status,
        SuggestionBoxId,
        CreatedAt
    FROM dbo.Suggestions;
GO

-- SuggestionComment: NEVER expose CommentAuthorId or CreatedBy.
CREATE OR ALTER VIEW dbo.vw_SuggestionComment
AS
    SELECT
        Id,
        SuggestionId,
        Text,
        IsAnonymous,
        CreatedAt
    FROM dbo.SuggestionComments;
GO

-- Vote: NEVER expose VoteAuthorId. Only Id + SuggestionId, used for COUNT/scope.
CREATE OR ALTER VIEW dbo.vw_Vote
AS
    SELECT
        Id,
        SuggestionId
    FROM dbo.Votes;
GO
