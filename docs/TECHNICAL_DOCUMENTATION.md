# Tehnička dokumentacija — Secure MCP Gateway (T7)

> **Rad:** *Implementacija bezbednog MCP gateway za mikroservisnu .NET platformu sa autorizacijom po
> alatu i audit tragom.*
> **Predmet:** dva nova .NET 8 servisa (`McpGateway`, `AiAssistantService`) + frontend za pregled
> audita, integrisani u postojeću Anonymous Reporting mikroservisnu platformu.
> **Prateći dokumenti:** `docs/THREAT_MODEL.md` (STRIDE), `.claude/guides/mcp-gateway.md` (detaljne
> bezbednosne odluke, evidence-backed).

---

## 1. Analiza problema

AI agenti (MCP klijenti) treba da pristupaju ograničenom skupu funkcija i podataka platforme, ali
**nekontrolisan pristup agenata živom, anonimnom sistemu je bezbednosni rizik** (de-anonimizacija,
cross-org curenje, neovlašćene izmene, zloupotreba kroz prompt-injection). Pravi problem nije „AI
iskustvo“ nego **bezbedno posredovanje pristupa**: autentifikovati agenta, **autorizovati svaki
pojedinačni poziv alata**, sve **evidentirati**, i formalno analizirati rizike (threat model). Srž
rada je **bezbednosni gateway**, agent je demonstracioni klijent.

## 2. Arhitektura rešenja

### 2.1 Topologija

Dva nova servisa sa jasnom granicom poverenja (detaljan dijagram: `docs/THREAT_MODEL.md` §2):

| Komponenta | Uloga | Ključni fajlovi |
|---|---|---|
| **`McpGateway`** (HERO) | Interni MCP kanal + JWT-zaštićen Audit API; per-tool authz + audit svake odluke | `Program.cs`, `Authorization/`, `Tools/`, `Audit/`, `Data/`, `Clients/` |
| **`AiAssistantService`** (demo agent) | MCP klijent gatewaya + Anthropic Claude klijent; `POST /api/AiChat` vrti agentic petlju | `Agent/AiChatAgent.cs`, `Clients/{ClaudeClient,McpGatewayClient}.cs`, `Auth/AgentTokenService.cs` |
| **Frontend** (`/admin`, admin+manager) | Audit dashboard (obavezno) + mali chat demo (propose-confirm HITL) | `Frontend/reporting-app/src/pages/{AuditDashboard,AiChatDemo}` |
| **nginx + Docker** | `/api/Audit/` + `/api/AiChat/` izloženi; `/mcp` interni | `gateway/nginx.conf`, `docker-compose.yml` |

### 2.2 Per-call bezbednosni pipeline (srž)

Za SVAKI poziv alata (`McpGateway/Program.cs` MCP filter → `ToolAuthorizationFilter` →
`RequestGatekeeper.AuthorizeAndAuditAsync`):

1. **Validacija agenta** — agent-JWT (RS256); gateway drži samo javni ključ → falsifikat nemoguć.
2. **Ekstrakcija korisnika** — iz OBO JWT claims (rola + org).
3. **Per-tool autorizacija** (`ToolAuthorizer.Authorize`) = `tool ∈ agent.AllowedTools` **AND**
   `user.role ∈ tool.AllowedRoles` **AND** org-scope zadovoljen → allow/deny + razlog. Deny-by-default.
4. **Audit** — svaka odluka (allow I deny) u `McpAuditDB`; **fail-closed** (neuspeh audita → deny).
5. **Izvršenje** (na allow) — read → SELECT-only scrubbed views (Dapper); write → postojeći REST +
   OBO bearer, uz gateway-side org-verify **pre** write-a; write ostaje `Proposed` (HITL).
6. **Obogaćivanje ishoda** — isti audit red se update-uje ishodom (best-effort, posle izvršenja).

### 2.3 Dual-principal identitet

Gateway autorizuje po **preseku** dva principala: **agent** (kriptografski, RS256 agent-JWT) ∩
**korisnik** (OBO — originalni OrganizationService JWT: rola + org). Audit beleži OBA → direktno
pokriva „evidentiranje aktivnosti korisnika **i** agenata“. Klijentska strana šalje oba u
`AdditionalHeaders` (`Authorization` = OBO, `X-Agent-Token` = agent-JWT, `X-Correlation-Id`).

### 2.4 Pristup podacima (hibrid)

- **Read:** direktan SQL (Dapper) preko **SELECT-only** naloga `mcp_read` ograničenog na **scrubbed,
  org-scoped views** (bez PII). Org-scope se **komponuje u gatewayu** (Problem/Suggestion nemaju
  `OrganizationId` — razreši `orgId → boxIds` iz box-view, pa filtriraj po tim boxIds).
- **Write:** postojeći REST `/api/*` sa prosleđenim OBO bearer-om (nasleđuje validaciju + `TryLogAsync`
  audit + T2/T3 fail-closed). Allow-lista akcija; enumi na žici kao INT.

---

## 3. Katalog alata i policy

12 alata (7 read + 5 write), jedan agent `ai-assistant`. Policy je konfigurabilna u
`McpGateway/appsettings.json` (`Gateway:Tools` / `Gateway:Agents`) — „granularno po alatu“.

| Klasa | Alati |
|---|---|
| Read (Admin+Manager, org-scoped) | `get_org_overview`, `get_problem_stats`, `get_suggestion_stats`, `list_boxes`, `search_submissions`, `get_submission_details` |
| Read (Admin-only, global) | `get_global_stats` |
| Write (Admin+Manager, org-scoped, HITL) | `set_box_status`, `update_submission_status`, `add_comment`, `set_box_password`, `create_box` |

## 4. Audit modul

Namenski `McpAuditDB` (EF Core). Zapis (`McpGateway/Audit/AuditEntry.cs`): oba principala
(`AgentId`/`UserId`/`UserRole`/`OrganizationId`), `ToolName`, scrubbed `ArgsSummary`, `Decision` +
`DecisionReason`, `Confirmation` (Proposed/Confirmed/Rejected za write), `Outcome`/`Error`/`DurationMs`
(obogaćeni posle izvršenja), `CorrelationId`. Izlaže `GET /api/Audit`
(`[Authorize(Roles="Admin,Manager")]`, scrubbed `AuditDTO`, filteri + bounded paging, force-org za
managera).

---

## 5. Pokrivenost „Očekivanih rezultata“ (Zadatak.docx)

| # | Očekivani rezultat | Realizovano čime | ✓ |
|---|---|---|---|
| 1 | Analiza problema + koncepti | §1 ovde + `docs/THREAT_MODEL.md` §1 | ✅ |
| 2 | Opis arhitekture | §2 (topologija, pipeline, dual-principal, data layer) | ✅ |
| 3 | MCP gateway servis | `McpGateway/` (Faza A–C) | ✅ |
| 4 | Mehanizam autorizacije po tool-u | `ToolAuthorizer` + dual-principal (§2.2–2.3) | ✅ |
| 5 | Audit modul *(opciono)* | Namenski `McpAuditDB` (§4) | ✅ |
| 6 | Frontend za pregled aktivnosti | AuditDashboard + AiChatDemo (Faza F) | ✅ |
| 7 | Threat model *(opciono)* | `docs/THREAT_MODEL.md` (STRIDE + prompt-injection) | ✅ |
| 8 | Testovi i evaluacija | §6 ovde (`SecurityEvaluationTests` + 156 zelenih) | ✅ |
| 9 | Dokumentacija + zaključak | Ovaj dokument + §8 | ✅ |

**Svih 9 pokriveno** (uključujući oba „opciona“).

---

## 6. Testovi i rezultati evaluacije

### 6.1 Brojevi (svi zeleni)

| Skup | Broj | Komanda |
|---|---|---|
| `McpGatewayTests` (86 postojećih + 18 novih security) | **104** | `dotnet test McpGatewayTests/McpGatewayTests.csproj` |
| `AiAssistantServiceTests` | **14** | `dotnet test AiAssistantServiceTests/AiAssistantServiceTests.csproj` |
| Frontend (utils/services čista logika, ispod routera) | **38** | `cd Frontend/reporting-app && CI=true npx react-scripts test <path> --watchAll=false` |
| **Ukupno** | **156** | |

> Pre-existing padovi (`OrganizationServiceTests` 14 + `AnonymousUserServiceTest` 6) su **nezavisni**
> od T7, postoje i na `origin/dev` (`[Authorize]` 401 / JSON empty-body drift) — nisu regresija.

### 6.2 Konsolidovan security suite

`McpGatewayTests/SecurityEvaluationTests.cs` (18 testova) je jedinstven „dokaz set“: svaki STRIDE red
iz threat modela ima imenovan test, na nivou koji najbolje dokazuje garanciju (pure-unit vs.
`WebApplicationFactory` integration). Mapiranje pretnja → test: vidi tabele u `docs/THREAT_MODEL.md`
§3–4. Ključni GAP-testovi dodati u Fazi G: expired-ali-validan OBO JWT → 401; injection-kroz-argument
→ odluka nezavisna od sadržaja; eksplicitan cross-org force-scope.

---

## 7. Bezbednosne procedure (ručni dokaz)

Dve garancije su na nivou infrastrukture koje in-memory test okvir ne može da enforce-uje, pa se
dokazuju dokumentovanom procedurom (izvor: `McpGateway/Sql/README.md`).

### 7.1 SELECT-only garancija (DB privilegije)

Nalog `mcp_read` fizički ne može da piše niti da čita sirove tabele/PII. Dokaz (`sqlcmd` kao
`mcp_read`):

```sql
-- view SELECT USPEVA:
SELECT TOP 1 * FROM ProblemBoxDB.dbo.vw_ProblemBox;              -- OK
-- base-table SELECT je DENY'd, write je strukturno nemoguć:
SELECT TOP 1 * FROM ProblemBoxDB.dbo.ProblemBoxes;              -- permission denied
UPDATE ProblemBoxDB.dbo.ProblemBoxes SET Name='x';             -- permission denied
```

Očekivano: prvi upit vraća red iz scrubbed view-a; druga dva vraćaju „permission denied“. Time je
dokazan **Elevation/Information-disclosure** red iz threat modela na DB nivou. Bootstrap (kreiranje
views + naloga) je post-up korak (`Sql/01_read_views.sql` + `Sql/02_read_login.sql`).

### 7.2 Funkcionalni end-to-end (puna granica poverenja)

Puna putanja: korisnik → agent → gateway (authz + audit) → REST/read → audit review. Traži pravi
`ANTHROPIC_API_KEY` + `AGENT_PRIVATE_KEY_PEM` (usklađen sa javnim ključem u appsettings) + SQL
bootstrap. Procedura i očekivani ishodi: `McpGateway/Sql/README.md` (sekcija „End-to-end smoke“).

Očekivano: read upit vraća odgovor sniman **isključivo** iz rezultata alata; `GET /api/Audit`
prikazuje jedan red po pozivu alata sa oba principala (agent + OBO korisnik), odlukom i ishodom;
neautentifikovan `POST /api/AiChat` → 401; interni `/mcp` nedostupan kroz nginx.

---

## 8. Zaključak — ograničenja i moguća unapređenja

### 8.1 Ograničenja (svesno van opsega)

- **Mid-session token refresh** ne postoji (nema interceptora na gateway/agent strani) — dugotrajan
  razgovor može isteći usred rada; ponašanje je fail-closed (401), ali prekida UX. Globalni dug
  platforme, ne samo T7.
- **Client-echo istorija razgovora** je granica poverenja: server je stateless, istorija dolazi od
  klijenta. Gateway to kompenzuje **autorizacijom svakog write-a** (HITL nije autorizaciona granica),
  ali istorija sama nije server-verifikovana.
- **Dev-secret inline konvencija** — SA lozinka i dev JWT ključevi su inline u compose-u (nasleđeno
  iz postojećeg repo-a). Prihvatljivo za demo; produkcija traži secret-manager. Tri istinski osetljiva
  ključa (`ANTHROPIC_API_KEY`, `AGENT_PRIVATE_KEY_PEM`, `MCP_READ_PASSWORD`) idu preko gitignored `.env`.
- **SELECT-only bootstrap je ručni post-up korak** — dok se ne izvrši, read alati fail-closed (Deny);
  gateway i Audit API svejedno rade.
- **DoS/cost ograde su na agent servisu**, ne na gatewayu — gateway nema sopstveni rate-limit
  (oslanja se na `AiAssistantService` cap-ove i nginx).

### 8.2 Moguća unapređenja

- **Streaming** odgovora agenta (trenutno se ceo odgovor vraća odjednom).
- **Multi-agent** / više agent-identiteta sa različitim allow-listama alata (policy to već podržava).
- **Policy-admin UI** — živo uređivanje per-tool autorizacije (3. frontend sloj).
- **Perzistentna chat baza** umesto client-echo istorije (uklonila bi granicu poverenja iz §8.1).
- **Automatizovan SQL init** (views + `mcp_read`) kroz init container umesto ručnog post-up koraka.
- **Mid-session refresh interceptor** na frontendu/agentu (rešava prvo ograničenje).

### 8.3 Rezime

Rad isporučuje **bezbednosni MCP gateway** koji autentifikuje agente (RS256) i korisnike (OBO JWT),
**autorizuje svaki pojedinačni poziv alata** (dual-principal per-tool policy, deny-by-default),
**evidentira svaku odluku** (allow i deny, oba principala, trajni AuditDB, fail-closed), izlaže
audit dashboard, i formalno analizira rizike kroz STRIDE threat model. Bezbednosna ispravnost je
dokazana kroz **156 zelenih testova** (uklj. konsolidovan STRIDE suite) + dokumentovane DB-nivo i
end-to-end procedure. Svih 9 očekivanih rezultata je pokriveno.
