# Guide: MCP Security Gateway (McpGateway)

`McpGateway` (.NET 8) is a security boundary that lets AI agents call a fixed set of tools against
the platform, authorizing and auditing every call. It is NOT the nginx gateway (`gateway/nginx.conf`).
Added in T7: Faza A = authz core; Faza B = real tool bodies. Read this before touching gateway code.

## Trust boundary & per-call pipeline
Agent (outside the boundary, holds only its own private key) → `POST /mcp` with
`Authorization: Bearer <user OBO JWT>` + `X-Agent-Token: <agent JWT>`. Per tool call,
`AddCallToolFilter` → `ToolAuthorizationFilter.EvaluateAsync` → `RequestGatekeeper.AuthorizeAndAuditAsync`:
validate agent-JWT (RS256; gateway holds only the agent's PUBLIC key → cannot forge it), extract the
user from OBO claims, run `ToolAuthorizer.Authorize` (per-tool policy = agent's allowed tools ∩ user role
+ org-scope), then audit the decision (allow AND deny). Deny-by-default; audit-write failure fails closed.

| Component | File |
|---|---|
| Per-call filter (MCP adapter) | `McpGateway/Authorization/ToolAuthorizationFilter.cs` |
| Orchestrator (authz + audit) | `McpGateway/Authorization/RequestGatekeeper.cs` |
| Per-tool decision (pure) | `McpGateway/Authorization/ToolAuthorizer.cs` |
| Agent-JWT (RS256) validation | `McpGateway/Authorization/AgentTokenValidator.cs` |
| Policy + agents (fail-fast) | `McpGateway/Authorization/PolicyStore.cs`, `appsettings.json` `Gateway:` |

## Read tools read FOREIGN DBs via Dapper + scrubbed views + a SELECT-only login — NEVER EF
The gateway reads ProblemDB/SuggestionDB/ProblemBoxDB/SuggestionBoxDB, which it does NOT own.

| Rule | Why | Evidence |
|---|---|---|
| Dapper, not EF | read layer doesn't own the schemas; EF would need ~4 keyless DbContexts or coupling to other services' contexts (which pulls in PII columns) | `McpGateway/Data/SqlReadRepository.cs` |
| SELECT-only login | `GRANT SELECT` on views only, `DENY SELECT` on base tables, no write grants → account physically cannot write or read raw/PII rows | `McpGateway/Sql/02_read_login.sql` |
| Scrubbed views only | omit all PII: `AnonymousUserId`, `*CommentAuthorId`, `VoteAuthorId`, `CreatedBy`; boxes expose `HasPassword` bit, never `Password` | `McpGateway/Sql/01_read_views.sql` |
| Parameterized always | LIKE pattern passed as a param value, `IN @boxIds` via Dapper list-expansion | `SqlReadRepository.cs` |

## Org-scope is composed in the gateway (no cross-DB joins)
`Problem`/`Suggestion` have NO `OrganizationId` — org lives on the box (a different DB). So resolve
`orgId → boxIds` from the box view, then filter submission views by those boxIds in `WHERE`.

| Rule | Detail |
|---|---|
| Manager forced / admin optional | `ToolScope.Resolve`: manager → `EffectiveOrganizationId` (requested org IGNORED); admin → optional `organizationId` arg (null = all). |
| null vs empty boxIds | `null` = admin/all (no filter); EMPTY list = scoped caller with zero boxes → return zero (NEVER `IN ()`, never "all"). |
| Scope in WHERE, not post-filter | repo methods take resolved boxIds; out-of-scope `get_submission_details` returns null → opaque "not found" (no existence leak). |

Evidence: `McpGateway/Tools/ToolScope.cs`, `McpGateway/Tools/ReadTools.cs`, `McpGateway/Data/IReadRepository.cs`.

## Carrying the authorized context into tool bodies
Tool bodies are static `[McpServerTool]` methods; they get the authorized org + OBO bearer via a
SCOPED `IInvocationContextAccessor` set by the filter (on allow, before `next()`). The filter is a
SINGLETON, so the accessor is passed as an `EvaluateAsync` parameter resolved per-request — NOT
ctor-injected into the singleton (which would capture a scoped service). Evidence:
`McpGateway/Authorization/InvocationContext.cs`, `Program.cs` (MCP filter lambda).

## Audit args are scrubbed deny-by-default
`ArgsSummary.Build` logs only whitelisted keys (`id/type/organizationId/status/priority`) literally;
every other arg (e.g. `keyword`) becomes `<len:N>`. The audit trail must never itself leak PII.
Evidence: `McpGateway/Audit/ArgsSummary.cs`.
