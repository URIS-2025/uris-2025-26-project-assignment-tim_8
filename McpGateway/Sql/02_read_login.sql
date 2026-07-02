-- =============================================================================
-- McpGateway — Least-privilege SELECT-only read login `mcp_read`
-- =============================================================================
-- SECURITY INTENT (backs the thesis: "the read account physically cannot write
-- or read base tables / PII")
--
--   * ONE server login `mcp_read`, mapped to a user in EACH of the 4 databases.
--   * GRANT SELECT is given ONLY on the scrubbed views from 01_read_views.sql.
--   * DENY SELECT is placed on the underlying base tables (defense-in-depth):
--     even if a view is dropped, altered, or a new query targets a raw table,
--     the account still cannot read raw rows / PII columns.
--   * NO INSERT / UPDATE / DELETE / EXECUTE is ever granted — the account is
--     structurally incapable of writing.
--
--   DENY overrides GRANT in SQL Server. The views own their base tables
--   (same schema/owner => ownership chaining), so SELECT on a view still
--   succeeds even though the base table is DENY'd — this is exactly the
--   intended asymmetry: read via the scrubbed view, never via the table.
--
--   PASSWORD: the placeholder below is replaced at deploy time (Docker /
--   Faza E injects the real secret via environment / init script). Never commit
--   a real password here.
-- =============================================================================


-- =============================================================================
-- 1. Server-level login (create once)
-- =============================================================================
USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'mcp_read')
BEGIN
    -- CHANGE_ME_IN_ENV: real password injected by Docker / Faza E deployment.
    CREATE LOGIN mcp_read WITH PASSWORD = 'CHANGE_ME_IN_ENV',
        CHECK_POLICY = ON;
END
GO


-- =============================================================================
-- 2. ProblemBoxDB — user + grant on view + deny on base table
-- =============================================================================
USE ProblemBoxDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'mcp_read')
    CREATE USER mcp_read FOR LOGIN mcp_read;
GO

GRANT SELECT ON OBJECT::dbo.vw_ProblemBox TO mcp_read;
-- Defense-in-depth: never allow reading the raw table (holds Password + all PII).
DENY SELECT ON OBJECT::dbo.ProblemBoxes TO mcp_read;
GO


-- =============================================================================
-- 3. SuggestionBoxDB — user + grant on view + deny on base table
-- =============================================================================
USE SuggestionBoxDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'mcp_read')
    CREATE USER mcp_read FOR LOGIN mcp_read;
GO

GRANT SELECT ON OBJECT::dbo.vw_SuggestionBox TO mcp_read;
DENY SELECT ON OBJECT::dbo.SuggestionBoxes TO mcp_read;
GO


-- =============================================================================
-- 4. ProblemDB — user + grant on views + deny on base tables
-- =============================================================================
USE ProblemDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'mcp_read')
    CREATE USER mcp_read FOR LOGIN mcp_read;
GO

GRANT SELECT ON OBJECT::dbo.vw_Problem        TO mcp_read;
GRANT SELECT ON OBJECT::dbo.vw_ProblemComment TO mcp_read;
DENY  SELECT ON OBJECT::dbo.Problems          TO mcp_read;
DENY  SELECT ON OBJECT::dbo.ProblemComments   TO mcp_read;
GO


-- =============================================================================
-- 5. SuggestionDB — user + grant on views + deny on base tables
-- =============================================================================
USE SuggestionDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'mcp_read')
    CREATE USER mcp_read FOR LOGIN mcp_read;
GO

GRANT SELECT ON OBJECT::dbo.vw_Suggestion        TO mcp_read;
GRANT SELECT ON OBJECT::dbo.vw_SuggestionComment TO mcp_read;
GRANT SELECT ON OBJECT::dbo.vw_Vote              TO mcp_read;
DENY  SELECT ON OBJECT::dbo.Suggestions          TO mcp_read;
DENY  SELECT ON OBJECT::dbo.SuggestionComments   TO mcp_read;
DENY  SELECT ON OBJECT::dbo.Votes                TO mcp_read;
GO

-- =============================================================================
-- No GRANT INSERT / UPDATE / DELETE / EXECUTE anywhere by design.
-- The account can only SELECT the scrubbed views above.
-- =============================================================================
