# Guide: MCP Security Gateway (McpGateway)

`McpGateway` (.NET 8) is a security boundary that lets AI agents call a fixed set of tools against
the platform, authorizing and auditing every call. It is NOT the nginx gateway (`gateway/nginx.conf`).
Added in T7: Faza A = authz core; Faza B = real tool bodies (B-read = scrubbed views; B-write =
REST + OBO). Read this before touching gateway code.

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
every other arg (e.g. `keyword`, `password`, `name`, `text`) becomes `<len:N>`. The audit trail must
never itself leak PII or secrets. Evidence: `McpGateway/Audit/ArgsSummary.cs`.

## Write tools call the EXISTING REST endpoints — gateway verifies org BEFORE the write
Write tools (`McpGateway/Tools/WriteTools.cs`) do NOT touch the DB. They POST/PUT the existing
`/api` endpoints via typed clients in `McpGateway/Clients/`, forwarding the caller's OBO bearer.
The REST endpoints have NO `[Authorize]`, so the gateway is the ONLY authorization barrier.

| Rule | Why | Evidence |
|---|---|---|
| Org-verify BEFORE the REST call | REST won't check org → gateway must, or a manager could write to a foreign box/submission | `WriteTools.BoxInScopeAsync` / `SubmissionInScopeAsync` |
| Reuse the read layer as the ownership check | an org-scoped read that returns null = not found OR out of scope → opaque `Deny()` (no existence leak) | `GetProblem/SuggestionUpdateFieldsAsync` (null=deny) |
| `create_box` FORCES the org | no pre-existing target to check → `ToolScope.Resolve(ctx, organizationId)` pins a manager to their own org (LLM arg ignored); admin MUST pass `organizationId` | `WriteTools.CreateBox` |
| Admin (org=null) skips membership | admin may target any box/submission by design | `BoxInScopeAsync` returns true when org is null |

## Write clients are NON-swallow (opposite of LoggerServiceClient)
`WriteClientHttp.SendAsync` mirrors the Faza-A `LoggerServiceClient` shape (named
`IHttpClientFactory` client, OBO bearer forward, linked ~5s timeout, STJ web opts, `virtual` +
parameterless ctor for Moq) with ONE deliberate difference: it never swallows. A write changes
state, so its outcome must be reported.

| Rule | Detail | Evidence |
|---|---|---|
| Return `WriteResult(Success, StatusCode, Detail)` | 2xx → `(true, code, null)`; non-2xx → `(false, code, detail)`; transport/timeout → `(false, 0, msg)` — never hidden | `McpGateway/Clients/WriteResult.cs` |
| Tool maps outcome to `CallToolResult` | 2xx → success; `409` → surfaced (inherits T2/T3 rule); other 4xx → status only; 5xx/transport → generic. Raw server body NEVER reaches the LLM | `WriteTools.Map` |
| Enums on the wire = INT | no `JsonStringEnumConverter` anywhere → `status`/`priority` sent as numbers (Active=0/Inactive=1; Priority Low=0/Med=1/High=2) | `WriteRequests.cs` |
| `update_submission_status` = read-modify-write | REST PUT takes a FULL DTO and overwrites unset fields (see `.claude/rules/repositories-and-dtos.md` §10) → read current fields, change only Status, send the full DTO | `WriteTools.UpdateSubmissionStatus` |
| `add_comment` author = OBO user | `Guid(ctx.UserId)`, `IsAnonymous=false`; non-GUID UserId → error, never `Guid.Empty` | `WriteTools.AddComment` |

Note: write tools return `CallToolResult` (with `IsError` on failure), unlike read tools which
return a JSON string — a write must signal failure at the protocol level. Audit of the write
OUTCOME is deferred to Faza C (Faza A already audits the `Proposed` decision).
