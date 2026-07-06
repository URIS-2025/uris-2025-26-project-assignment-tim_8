# McpGateway — SQL read layer bootstrap (T7 Faza E)

The McpGateway read tools query **scrubbed, org-scoped views** through a dedicated
**SELECT-only** SQL Server login `mcp_read`. That login and its views are NOT created by EF
migrations (EF only owns each service's own schema) — they must be created **once**, after the
stack is up, with this two-step bootstrap.

| Script | What it does | Runs as |
|--------|--------------|---------|
| `01_read_views.sql` | Creates the scrubbed `vw_*` views (omit all PII) in the 4 read DBs | `sa` |
| `02_read_login.sql` | Creates login `mcp_read`, GRANTs SELECT on the views, DENYs the base tables | `sa` |

## Ordering (important)

The views in `01_read_views.sql` reference the base tables (`dbo.ProblemBoxes`, `dbo.Problems`,
`dbo.Suggestions`, …). Those tables are created by each service's `Database.Migrate()` on startup.
So the bootstrap must run **after** `docker-compose up` and **after** the problem/suggestion/box
services have finished migrating (a few seconds). Until then the read tools fail **closed** (an
invalid `mcp_read` login → tool error + Deny audit), which is the intended fail-safe — the gateway
itself still boots and the Audit API works.

## The `mcp_read` password lives in ONE place: `.env`

`02_read_login.sql` ships with the literal placeholder `CHANGE_ME_IN_ENV`. The real password is
`MCP_READ_PASSWORD` from your gitignored `.env` (see `.env.example`). The **same** value is already
interpolated into the gateway's `ReadDb__*` connection strings in `docker-compose.yml`, so the
login you create here and the login the gateway connects with stay in sync. Never commit the real
password — substitute it at run time as shown below.

> SQL Server `CHECK_POLICY = ON` requires a strong password (≥8 chars, mixed case + digit/symbol).

## Bootstrap — PowerShell (Windows)

```powershell
# 0. Stack up; give the services ~30s to migrate their schemas first.
docker-compose up -d
Start-Sleep -Seconds 30

# 1. Create the scrubbed views (as sa). The 2022 image ships mssql-tools18 (needs -C to trust cert).
Get-Content McpGateway/Sql/01_read_views.sql | `
  docker exec -i sql-server /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'tim8urisPassword!' -C

# 2. Create the SELECT-only login, substituting the real password from .env.
$pw = (Get-Content .env | Select-String '^MCP_READ_PASSWORD=').ToString().Split('=',2)[1]
(Get-Content McpGateway/Sql/02_read_login.sql -Raw).Replace('CHANGE_ME_IN_ENV', $pw) | `
  docker exec -i sql-server /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'tim8urisPassword!' -C
```

## Bootstrap — bash

```bash
docker-compose up -d
sleep 30

docker exec -i sql-server /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'tim8urisPassword!' -C < McpGateway/Sql/01_read_views.sql

export $(grep '^MCP_READ_PASSWORD=' .env | xargs)
sed "s/CHANGE_ME_IN_ENV/${MCP_READ_PASSWORD}/g" McpGateway/Sql/02_read_login.sql | \
  docker exec -i sql-server /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'tim8urisPassword!' -C
```

> If the image has the older tools, use `/opt/mssql-tools/bin/sqlcmd` (no `-C`).

## Verify the SELECT-only guarantee

```sql
-- As mcp_read: a view SELECT succeeds…
sqlcmd -S localhost -U mcp_read -P '<MCP_READ_PASSWORD>' -C -Q "SELECT TOP 1 * FROM ProblemBoxDB.dbo.vw_ProblemBox;"
-- …but a base-table SELECT is DENY'd, and any write is structurally impossible:
sqlcmd -S localhost -U mcp_read -P '<MCP_READ_PASSWORD>' -C -Q "SELECT TOP 1 * FROM ProblemBoxDB.dbo.ProblemBoxes;"   -- expect: permission denied
sqlcmd -S localhost -U mcp_read -P '<MCP_READ_PASSWORD>' -C -Q "UPDATE ProblemBoxDB.dbo.ProblemBoxes SET Name='x';"   -- expect: permission denied
```

---

# End-to-end smoke procedure (documented — T7 Faza E)

Full trust-boundary path: user → agent → gateway (authorize + audit) → REST/read → audit review.
Requires a real `ANTHROPIC_API_KEY` + `AGENT_PRIVATE_KEY_PEM` in `.env` (the agent private key must
match `Gateway:Agents[0].PublicKeyPem` in `McpGateway/appsettings.json`).

```bash
# 1. Configure secrets and bring the stack up.
cp .env.example .env      # then fill ANTHROPIC_API_KEY, AGENT_PRIVATE_KEY_PEM, MCP_READ_PASSWORD
docker-compose up -d
# 2. Run the read-login bootstrap above (views + mcp_read).

# 3. Get a real OBO token: log in as an Admin/Manager via the OrganizationService.
TOKEN=$(curl -s http://localhost/api/User/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"<manager-email>","password":"<password>"}' | python -c "import sys,json;print(json.load(sys.stdin)['accessToken'])")

# 4. Ask the agent something read-only → gateway authorizes + audits each tool call.
#    NOTE the trailing slash: nginx `location /api/AiChat/` is a prefix match and only matches
#    paths that include the trailing slash — `/api/AiChat` (no slash) would not route.
curl -s http://localhost/api/AiChat/ \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"message":"How many active problem boxes does my organization have?"}'

# 5. Review the audit trail (Admin/Manager only) → shows agent + user + tool + Allow/Deny + reason.
curl -s http://localhost/api/Audit/ -H "Authorization: Bearer $TOKEN"
```

Expected: step 4 returns a grounded answer sourced only from tool results; step 5 lists one audit
row per tool call with both principals (agent + on-behalf-of user), the decision, and the outcome.
An unauthenticated `POST /api/AiChat` (no bearer) returns 401; the internal `/mcp` channel is not
reachable through nginx at all.
