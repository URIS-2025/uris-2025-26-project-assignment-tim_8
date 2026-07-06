# Guide: MCP Security Gateway (McpGateway)

`McpGateway` (.NET 8) is a security boundary that lets AI agents call a fixed set of tools against
the platform, authorizing and auditing every call. It is NOT the nginx gateway (`gateway/nginx.conf`).
Added in T7: Faza A = authz core; Faza B = real tool bodies (B-read = scrubbed views; B-write =
REST + OBO); Faza C = durable AuditDB + GET /api/Audit + outcome enrichment. Read this before
touching gateway code.

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

## Audit is durable + enriched with the execution outcome (Faza C)
Every decision is persisted to a dedicated EF Core store (`McpAuditDB`) via `SqlAuditSink`, then the
SAME row is enriched with the tool's execution outcome. The audit-review dashboard reads it through
`GET /api/Audit`.

| Rule | Why | Evidence |
|---|---|---|
| Durable sink is a SINGLETON over `IDbContextFactory<AuditDbContext>` | `RequestGatekeeper` (which consumes `IAuditSink`) is a singleton — a scoped `DbContext` would be a captive dependency. The factory makes a fresh context per op. | `Audit/SqlAuditSink.cs`, `Program.cs` (`AddDbContextFactory` + `AddSingleton<IAuditSink, SqlAuditSink>`) |
| Decision write FAIL-CLOSED; outcome enrichment BEST-EFFORT | The decision is recorded BEFORE the tool runs — a failed `RecordAsync` → Deny (no action without a record). The outcome is written AFTER via `UpdateOutcomeAsync` — the action already ran, so a failed enrichment is logged + swallowed, never fails the call. | `RequestGatekeeper.{AuthorizeAndAuditAsync,EnrichOutcomeAsync}`, `ToolAuthorizationFilter` |
| Only 3 audit fields are mutable | `Outcome`/`Error`/`DurationMs` are `set` (enriched in place); identity/decision fields stay `init`. Settable — not raw `ExecuteUpdate` — because the InMemory test provider can't run it. | `Audit/AuditEntry.cs` |
| Tool exception → enrich as Error + RETHROW | A crashing tool is recorded (`Outcome=Error`) and the exception propagates — never hidden. | `ToolAuthorizationFilter` |
| `Error` stores the tool's SANITIZED text only | Same message the tool returned to the agent (bounded); never a raw 5xx body/PII — the audit trail must not itself leak. | `ToolAuthorizationFilter.SanitizedText` |

### GET /api/Audit — force-org + one source of truth for "admin"
`AuditController` (`[Authorize(Roles="Admin,Manager")]`) returns a scrubbed `AuditDTO` (never the
entity) with filters + bounded-offset paging (pageSize ≤ 100, overflow-safe skip). Force-org: a
manager is pinned to their token's `OrganizationId` (a client-supplied org is ignored; null-org
records are admin-only). "Admin" is matched CASE-INSENSITIVELY against `ToolAuthorizer.AdminRole` —
the SAME constant the tool path uses — so the two never disagree on who is an admin (role titles are
free-form `UserRole.Title`; a case-sensitive check would fail-open under a role-title change).
Evidence: `Controllers/AuditController.cs`, `Authorization/ToolAuthorizer.cs`.

### Testing without a real DB
Under the `Testing` environment the AuditDB uses the InMemory provider and startup migration is
skipped; the integration `WebApplicationFactory` boots via
`WithWebHostBuilder(b => b.UseEnvironment("Testing"))`. Migrations are scaffolded through a
design-time `IDesignTimeDbContextFactory` so `dotnet ef` never runs the host.
Evidence: `Program.cs`, `Context/AuditDbContextFactory.cs`, `GatewayIntegrationTests`.

## Demo agent (AiAssistantService) — Faza D
`AiAssistantService` (.NET 8) is the demonstration MCP **client** of the gateway + an Anthropic
Claude client. `POST /api/AiChat` (`[Authorize]`, caller's OBO JWT) runs an agentic loop. SDK/MCP
types are confined to `Clients/ClaudeClient.cs` and `Clients/McpGatewayClient.cs` so the loop
(`Agent/AiChatAgent.cs`) is unit-testable with mocks (`IClaudeAgentClient` / `IMcpGatewayClient`).
Conversation state is client-side (echoed `History`) → the server is stateless; audit stays in the
gateway (this service has NO DbContext / no `TryLogAsync`).

### Dual-principal from the CLIENT side (AdditionalHeaders)
Per tool call the agent opens a FRESH MCP client (gateway is `Stateless=true`, OBO is per-request):
`McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions { Endpoint=<url>/mcp,
TransportMode=HttpTransportMode.StreamableHttp, AdditionalHeaders={ ["Authorization"]=<OBO>,
["X-Agent-Token"]=<agent-JWT>, ["X-Correlation-Id"]=<corr> } }))`. `AdditionalHeaders` is the ONLY
place the two principals + correlation id ride. The agent-JWT is minted RS256 by
`Auth/AgentTokenService.cs` (private key secret; the gateway holds only the public key).

| Concern | Fact (verified against the packages) |
|---|---|
| Anthropic SDK | official NuGet package `Anthropic` (NOT a community pkg) — `AnthropicClient`, `client.Messages.Create`, types in `Anthropic.Models.Messages`. Pass model as a STRING (`"claude-sonnet-5"`); `ToolChoiceAuto { DisableParallelToolUse = true }`; `Tool.InputSchema` via target-typed `new() { Properties, Required }`; `MessageCreateParams.System` is init-only; response blocks via `block.TryPickText/TryPickToolUse`. |
| MCP client SDK | NuGet package `ModelContextProtocol`; the client types (`McpClient`, `HttpClientTransport`, `HttpClientTransportOptions`, `HttpTransportMode`) live in `ModelContextProtocol.Core`. `ListToolsAsync()` → `McpClientTool.JsonSchema` (input schema); `CallToolAsync(name, IReadOnlyDictionary<string,object?>)` → `CallToolResult.Content.OfType<TextContentBlock>()` + `.IsError`. |

### Propose-confirm HITL is FAIL-SAFE (read allow-list, not a write deny-list)
`AiChatAgent` auto-executes a tool ONLY if it is on the `Agent:ReadTools` allow-list; every other
tool — writes AND any tool not listed (e.g. one added to the gateway later) — is returned as a
proposal for human confirmation, never auto-run (`if (!_readTools.Contains(name)) PendingConfirmation(...)`).
A write DENY-list would silently drift from the gateway's `IsWrite` policy and let a new write
auto-execute; the read ALLOW-list fails safe (unknown → confirm). The proposal's `ArgsSummary` masks
every non-whitelisted key so a box password is never surfaced. Evidence: `Agent/AiChatAgent.cs`,
`appsettings.json` `Agent:ReadTools`.

### CorrelationId is populated here (was null through Faza C)
The gateway's MCP filter reads `X-Correlation-Id`, parses to `Guid?` (fail-safe → null), and threads
it to `RequestGatekeeper.AuthorizeAndAuditAsync(correlationId:)` (the parameter existed since Faza C,
just unfilled). The agent sends ONE id per logical conversation across the whole tool-call chain and
echoes it back on confirm. Evidence: `McpGateway/Program.cs`, `McpGateway/Authorization/ToolAuthorizationFilter.cs`.

### Fail-closed WITHOUT leaking
`AiChatController`'s catch returns a FIXED message + `ILogger.LogError(ex)` — NOT `ex.Message`
(Claude/MCP/transport/crypto exceptions can carry internal topology like the gateway URL).
`OperationCanceledException` (client abort) is rethrown, not shaped as a 400. Caps: rate-limit
10/min per user, iteration cap 8, `max_tokens` 4096, `disable_parallel_tool_use`. Secrets (agent RSA
private key, Anthropic API key) come from env/user-secrets with startup fail-fast guarded by the
Testing env. Evidence: `AiAssistantService/{Controllers/AiChatController.cs, Program.cs, Agent/AiChatAgent.cs}`.
