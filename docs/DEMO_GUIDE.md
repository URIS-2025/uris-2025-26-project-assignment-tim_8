# Demo & test guide — Anonymous Reporting System + T7 Secure MCP Gateway

For a live walkthrough in front of an examiner. Every command and route here was executed against the
running stack on 2026-07-29; timings are measured, not estimated.

**Read order:** §0 strategy → §1 pre-flight → §2–§5 the walkthrough → §6 Q&A → §7 recovery.

---

## §0 — Strategy: how to run this in front of a professor

**Lead with the thesis, not the CRUD.** Your contribution is the secure MCP gateway. The reporting
system is the substrate it protects. If you spend 20 minutes on box CRUD you will run out of time
before the part being graded. Suggested split for a 30-minute slot:

| Minutes | What |
|---|---|
| 0–3 | One-paragraph framing: what the system is, what the thesis adds, why it matters |
| 3–10 | Base product, fast — one anonymous submission, one admin view (§2) |
| 10–22 | **The gateway: audit trail + AI assistant + HITL gate (§3)** ← the graded part |
| 22–27 | Prove the security claims break correctly (§4) |
| 27–30 | Tests + questions (§5, §6) |

**Four rules that matter more than the script:**

1. **Do the boot before he walks in.** Cold boot to healthy is ~44 s, but the SQL bootstrap and an
   nginx restart are involved (§1). Never do infrastructure in front of an audience.
2. **Show a failure on purpose.** A security thesis that only shows the happy path is unconvincing.
   §4 exists so *you* choose which failures he sees, rather than discovering one live.
3. **Name your limitations before he finds them.** §6 lists the questions this system genuinely
   invites. Answering "yes, and here is exactly why" reads as mastery; being caught reads as an
   oversight. Two of them are things a curious examiner *will* hit.
4. **Narrate, never fix silently.** If something breaks, say what you think it is, then fix it. §7.

**Framing paragraph you can adapt:**

> "This is an anonymous reporting platform — 11 ASP.NET Core microservices behind an nginx gateway,
> with a React frontend. Employees submit problems and suggestions anonymously; organizations manage
> them. My thesis work is the layer on top: an MCP gateway that lets an AI agent operate on this data
> **without** being trusted. Every tool call is authorized per-tool against two principals — the agent
> and the human it acts for — and every call lands in a durable audit trail. Reads never touch real
> tables; writes never happen without a human decision. I will show it working, then show it refusing."

---

## §1 — Pre-flight (do this before he arrives)

### The evening before

```bash
# 1. Back up the data volume — down -v destroys logins, and register is rate-limited 3/hour
docker compose stop
MSYS_NO_PATHCONV=1 docker run --rm \
  -v uris-2025-26-project-assignment-tim_8_sql-data:/from:ro -v "/c/backups":/to \
  alpine tar czf /to/sql-data-backup.tgz -C /from .

# 2. Verify the archive (a truncated backup is worse than none) — expect ~18
MSYS_NO_PATHCONV=1 docker run --rm -v "/c/backups":/bk:ro \
  alpine tar tzf /bk/sql-data-backup.tgz | grep -c '\.mdf'

# 3. Full test run so you know today's numbers (see §5)
```

### 30 minutes before

```bash
docker compose up -d
docker compose ps --format "table {{.Name}}\t{{.State}}"     # wait for 15/15 running
docker compose restart gateway                               # MANDATORY — see below
cd Frontend/reporting-app && npm start                       # serves :3000
```

Then confirm:

```bash
curl -s -o /dev/null -w '%{http_code}\n' http://localhost/health         # 200
curl -s -o /dev/null -w '%{http_code}\n' http://localhost/api/Problem/   # 200
```

> **Why `restart gateway` is mandatory.** nginx resolves upstream hostnames **once at startup** and
> caches the IPs. Any service that finishes booting later gets an address nginx never saw. Measured on
> a real cold boot: nginx up at 09:48:19, services at 09:48:38 — and `/api/Problem/`,
> `/api/Organization/`, `/api/Logger/` all returned **404** until the restart. Three of your routes are
> silently dead without it.

### Only if you ran `down -v`

The scrubbed `vw_*` views and the SELECT-only `mcp_read` login live in the **volume**, not in EF
migrations. After a `down -v` you must re-run both bootstrap scripts — see `McpGateway/Sql/README.md`.
Without them every read tool fails closed, which is *correct* but looks like a broken product.

### Checklist

| | |
|---|---|
| `.env` filled | `ANTHROPIC_API_KEY`, `AGENT_PRIVATE_KEY_PEM`, `MCP_READ_PASSWORD` |
| Agent keypair matches | private key in `.env` pairs with `Gateway:Agents[0].PublicKeyPem` — mismatch ⇒ every tool call Denies |
| Admin login works | both T7 pages are `allowedRoles={['admin','manager']}` |
| Anonymous account ready | pre-create one; signup is captcha-gated and `register` is 3/hour |
| Anthropic credit | real key; loop caps at 8 iterations, 10 req/min per user |
| Demo data | organizations, at least one box with submissions, and audit rows so the dashboard isn't empty |
| Browser zoom | 125–150% — projector legibility beats fitting more on screen |
| Second terminal open | for §4's curl probes; don't alt-tab hunting for one |

---

## §2 — Base product (fast — 7 minutes)

Goal: establish that the thing being protected actually works. Resist going deeper.

### 2.1 Anonymous submission — the core use case

| Step | Route | What to say |
|---|---|---|
| 1 | `/` | "Public landing page." |
| 2 | `/anonymous/login` | Log in with your pre-made anonymous account. Point out the **captcha** — "Cloudflare Turnstile on all four auth endpoints, fail-closed: if verification fails, the request is refused." |
| 3 | `/portal` | Pick an organization → the box list loads for that org. |
| 4 | `/portal` | Pick a **password-protected** box. A password field appears. "Box-level passwords — a second gate beyond authentication. Enforced server-side with BCrypt, not just hidden in the UI." |
| 5 | `/portal` | Submit a problem or suggestion. |
| 6 | `/portal` | Browse existing suggestions in a box. |

**Points worth making while here:**

- **Anonymity is structural.** Submissions carry no author identity; the anonymous account exists only
  to gate access, and reads by the AI go through views that omit PII entirely (§3.1).
- **Inactive boxes reject submissions** with 409 — a closed box refuses server-side, not by hiding a
  button.

### 2.2 Admin side

Log out, then log in at `/login` as admin or manager.

| Route | Shows |
|---|---|
| `/admin/dashboard` | Overview |
| `/admin/problems`, `/admin/suggestions` | Box lists — **create** a box (with an optional password) and **delete** one. These are live API calls. |
| `/admin/community` | Community board |
| `/admin/submissions/:id` | A submission with comments |
| `/admin/users` | **admin-only** route — mention that a manager cannot reach it |
| `/admin/billing` | Subscriptions and payments |

**Role scoping is worth 20 seconds:** a manager is scoped to their own organization in the UI, an
admin is not. That same distinction is enforced again, independently, inside the gateway (§4.2).

> ⚠️ **Do not demo `/admin/boxes/:id/settings`.** That page still contains hardcoded placeholder
> categories (`CAT-001 Workplace Safety`, …) rather than live data. Everything else listed above is real.

---

## §3 — The thesis: Secure MCP Gateway (12 minutes — the graded part)

### 3.1 Frame the architecture first (2 min, no clicking)

Say this before showing anything, because it makes the rest legible:

> "An AI agent needs to read and act on this data. The naive approach gives the model a database
> connection or an admin token. I treat the agent as **untrusted**. It talks to an MCP gateway that
> enforces four things:
>
> 1. **Dual-principal authentication** — the agent presents its own RS256-signed JWT *and* the
>    on-behalf-of token of the human using it. Missing either one, the call is denied.
> 2. **Per-tool authorization** — a policy per tool: which roles, org-scoped or not, read or write.
>    Unknown tool ⇒ deny by default.
> 3. **Reads cannot see PII and cannot write.** They go through scrubbed SQL views via a
>    SELECT-only database login. The account is *physically incapable* of writing — not trusted not to.
> 4. **Writes reuse the existing REST API**, so they inherit its validation and business rules, and
>    the gateway verifies organization ownership *before* the write.
>
> Every call — allowed or denied — produces a durable audit row naming both principals."

### 3.2 Audit dashboard — `/admin/audit`

Sidebar → **Audit Log**.

| Show | Say |
|---|---|
| The table | "Every tool call. Time, **agent**, **user**, role, tool, read/write, decision, outcome, duration." |
| The `Deny` rows | "These are real denials — the agent token failed validation, so the call never ran." |
| Filter → `Odluka: Deny` | "The audit is queryable, because an audit you can't query isn't an audit." |
| Click a row → modal | "Both principals. The decision *and its reason*. The scrubbed arguments. The outcome and duration." |
| The `Sažetak argumenata` field | **This is the good bit** — arguments are scrubbed **deny-by-default**. A comment body shows as `text=<len:10>`. "The audit proves what happened without becoming a copy of the sensitive data." |
| Pagination footer | "Server-side paging — bounded, so a large audit can't be used to exhaust the service." |

### 3.3 AI assistant, read path — `/admin/ai-chat`

Ask: **"Koliko aktivnih problem box-ova ima moja organizacija?"**

What to point at, in order:

1. The **tool chip** (`list_boxes`) — "the model chose a tool; the gateway authorized it before it ran."
2. The answer — "grounded in the tool result, not the model's memory."
3. Go back to **Audit Log** and refresh — "and here is that call, with both principals, from seconds ago."

That round trip — ask, answer, audited — is the single most persuasive 60 seconds you have. Do it slowly.

### 3.4 The HITL gate — the centrepiece

Ask: **"Deaktiviraj problem box koji se zove NewBox."**

Watch what happens, and narrate it:

1. The agent **first** calls `list_boxes` (a read — auto-executed) to resolve the name to an id.
2. It then **proposes** `set_box_status` and **stops**. A confirmation card appears with the exact tool
   and arguments, and **the input box locks** ("Prvo odlučite o predlogu iznad…").
3. Click **Odbij**. → "Nothing happened. The box is unchanged."
4. Ask again, then click **Odobri i izvrši**. → The write executes and the box is deactivated.
5. Return to **Audit Log** — the write is there with its outcome.

**Two things to say here that show you understand your own design:**

- "The gate is a **read allow-list**, not a write deny-list. Anything not explicitly on the
  auto-execute list — writes, *and any tool added later* — requires a human. It fails safe by
  construction, not by remembering to list dangerous things."
- "Notice `set_box_status` isn't in the admin UI at all. The gateway exposes capabilities the UI
  doesn't, which is exactly why it needs its own authorization layer rather than inheriting the UI's."

---

## §4 — Prove it refuses (5 minutes)

This is what separates a demo from an argument. Use the second terminal.

### 4.1 The trust boundary

```bash
# The AI chat endpoint requires an authenticated user
curl -s -o /dev/null -w '%{http_code}\n' -X POST http://localhost/api/AiChat/     # 401

# So does the audit API
curl -s -o /dev/null -w '%{http_code}\n' http://localhost/api/Audit/              # 401

# The MCP channel itself is NOT routable from outside at all
curl -s -o /dev/null -w '%{http_code}\n' http://localhost/mcp                     # 404
```

> "The MCP endpoint has no nginx route by design. Only the AI service, inside the Docker network,
> can reach it. An attacker on the host cannot talk to the gateway directly even with a valid user
> token."

### 4.2 The SELECT-only guarantee — the strongest single demo

```bash
pw='<MCP_READ_PASSWORD from .env>'

# The scrubbed view: allowed
MSYS_NO_PATHCONV=1 docker exec -i sql-server /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U mcp_read -P "$pw" -C -Q "SELECT TOP 1 * FROM ProblemBoxDB.dbo.vw_ProblemBox;"

# The real table: DENIED
MSYS_NO_PATHCONV=1 docker exec -i sql-server /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U mcp_read -P "$pw" -C -Q "SELECT TOP 1 * FROM ProblemBoxDB.dbo.ProblemBoxes;"

# Writing: DENIED
MSYS_NO_PATHCONV=1 docker exec -i sql-server /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U mcp_read -P "$pw" -C -Q "UPDATE ProblemBoxDB.dbo.ProblemBoxes SET Name='x';"
```

> "This is the account the AI's read path uses. It can read the scrubbed view and nothing else.
> Prompt injection cannot make it read a base table or write, because the *database* refuses —
> there is no code path to bypass."

### 4.3 A refusal that is honest in the audit

In the chat, ask for a submission that doesn't exist (or belongs to another org):

**"Prikaži detalje prijave tipa problem sa id 11111111-2222-3333-4444-555555555555."**

The agent reports it can't be found *or is out of scope* — deliberately ambiguous. Then open the audit:
that row reads `Decision=Allow, Outcome=Error`.

> "Two things. The refusal is **opaque** — 'not found' and 'not yours' are indistinguishable, so I
> never leak whether a record exists. And the audit records it as an **error**, not a success, so a
> reviewer can find refusals by filtering. That second part was a bug I found and fixed during
> pre-defense testing: it used to log as `Success`."

Saying that last sentence is a strength, not a weakness. It shows you tested your own audit trail.

### 4.4 If he asks for cross-organization access

Log in as a **manager** (not admin) and ask the assistant about another organization's data. The
manager's organization comes from their token; the `organizationId` argument the model supplies is
**ignored**. Scope is composed gateway-side and applied in the SQL `WHERE`.

---

## §5 — Tests (3 minutes)

```bash
dotnet test McpGatewayTests/McpGatewayTests.csproj              # 110 pass
dotnet test AiAssistantServiceTests/AiAssistantServiceTests.csproj  # 18 pass
```

`McpGatewayTests` includes `SecurityEvaluationTests` — a STRIDE-mapped suite where each test maps to a
threat in `docs/THREAT_MODEL.md`. Open that file alongside; the mapping is the artifact, not the count.

**Be upfront about the known-red suites** before he runs them himself:

```bash
dotnet test OrganizationServiceTests/OrganizationServiceTests.csproj   # 88 pass, 14 fail
dotnet test AnonymousUserServiceTest/AnonymousUserServiceTest.csproj   # 48 pass,  6 fail
```

> "Those 20 failures are pre-existing on `dev` and unrelated to my work — `[Authorize]` and
> empty-body assertions in older integration tests. I kept them visible rather than deleting them, and
> I verified my changes added none: 515 passing, the same 20 failing before and after."

Full totals if asked: **515 passing across 8 projects**, frontend build compiles plus 38 frontend tests.

---

## §6 — Questions this system invites (prepare these)

The first two are things a curious examiner will actually hit. Know them cold.

| Question | Answer |
|---|---|
| **"Where is the human gate enforced — gateway or client?"** | **In the agent, not the gateway.** `AiChatAgent` auto-executes only tools on the read allow-list; everything else returns a proposal, so the gateway isn't called until a human approves. The gateway independently authorizes and audits every call it does receive. `THREAT_MODEL.md` §2.1 states this explicitly. |
| **"This audit row says `Proposed` but also `Success` — didn't it need confirmation?"** | The confirmation happened in the agent *before* the gateway was called. `Confirmation` records that the call arrived as a proposal; the gateway then authorized and executed it. (I corrected the threat model during pre-defense testing — an earlier version claimed the gateway itself withheld execution, which the code does not do.) |
| **"Where is a *rejected* action recorded?"** | Client-side only today. Reject never reaches the gateway, so there's no audit row. Known limitation; `Confirmed`/`Rejected` are reserved states. The fix is a small confirmation endpoint on the gateway — deliberately not attempted days before the defense. |
| **"What stops the client lying about a confirmation?"** | Nothing in the agent — but nothing is gained. The gateway re-authorizes independently against the user's own token and organization scope. It's "the human bypassed the LLM", not privilege escalation. Conceded in §2.1. |
| **"Can the human really consent if arguments are masked?"** | Not fully, for sensitive keys. Approving `set_box_password` shows `password=<hidden>`, so the consent is obtained but not informed. Safe keys (id, type, status) *are* shown in full. Correct fix is a shaped redaction (`•••••• (8 chars)`). |
| **"Is dual-principal enforced everywhere?"** | On `tools/call`, yes. `tools/list` needs only the user token — so an authenticated user could enumerate the catalog. Contained because `/mcp` has no external route. Worth tightening. |
| **"Why does the captcha say 'For testing only'?"** | Cloudflare's always-passes **test** sitekey. A production key drops in with no code change. |
| **"What happens if the audit database is down?"** | The authorization **fails closed** — if the audit write fails, the call is denied. No action without a record. That's `RequestGatekeeper`, and it's deliberate. |
| **"Why not let the AI write SQL directly?"** | Writes go through the existing REST endpoints so they inherit validation, business rules, and the box-status/password gates. A SQL-writing agent would bypass all of it. And the read account can't write at all. |
| **"Aren't you just trusting the model to behave?"** | No — that's the point. Every control is outside the model: signed identity, per-tool policy, a SELECT-only account, gateway-side org verification, human confirmation, durable audit. Prompt injection can make the model *ask*; none of it changes what the gateway *allows*. |

---

## §7 — If something breaks

Narrate, then fix. An explained hiccup costs nothing; silence costs credibility.

| Symptom | Say | Do |
|---|---|---|
| Routes return 404 after boot | "nginx cached upstream IPs from before the services finished starting." | `docker compose restart gateway` |
| Audit table empty | "The audit is a separate durable database — empty until the first tool call." | Run one chat query (§3.3) |
| Read tools error | "The read layer uses a SELECT-only account whose views are a manual bootstrap step — it's failing closed, as designed." | Run the SQL bootstrap |
| Chat hangs or 429 | "Rate limit is 10 requests per minute per user, deliberately." | Wait, retry |
| Chat shows "Asistent trenutno ne moze da obradi zahtev." | "The user-facing message is fixed by design so it can't leak internals — the cause is in the service log." | `docker logs ai-assistant-service --tail 30` |
| Containers crash-loop on cold boot | "Services race to `CREATE DATABASE` on a shared SQL Server; it self-heals in a couple of restart cycles." | Wait for 15/15 |
| `npm start` fails, `react-scripts` not recognized | "OneDrive breaks the node_modules symlinks." | `npm install`, or serve the prebuilt `build/` |

**The one thing to avoid:** don't run `docker compose down -v` to "reset" something mid-demo. It
destroys every database including the account you log in with, and `register` is rate-limited to 3
per hour. There is no quick recovery.

---

## Appendix — measured reference

| Metric | Value |
|---|---|
| Docker engine launch → ready | ~30 s |
| `up -d` → 15/15 running (warm volume) | 22 s |
| Cold boot → healthy incl. gateway restart | **44 s** |
| Backend tests | 515 pass / 20 known-red |
| `McpGatewayTests` / `AiAssistantServiceTests` | 110 / 18 |
| Frontend | build compiles + 38 tests |
| Tool-call latency (observed) | 3–605 ms reads; ~1.3–2.6 s writes |

Cross-references: `docs/THREAT_MODEL.md`, `McpGateway/Sql/README.md` (bootstrap + smoke),
`.claude/guides/mcp-gateway.md` (design), `.demo/runsheet-final.md` (condensed click path).
