# Rule: Gateway Routing (Nginx)

Every controller in every backend service **must** have a matching `location /api/<Controller>/`
block in `gateway/nginx.conf` before it is reachable from the frontend or any external caller.
This is the single most common cause of "404 from frontend but controller exists in C#."

---

## Why frontend calls `http://localhost/api/*`

Only two services are published to the host:

| Service | Published port |
|---|---|
| `gateway` (Nginx) | `80:80` — all external traffic enters here |
| `sql-server` | `1433:1433` — direct DB access for local dev tools |

All 11 backend services use `expose: "8080"` in `docker-compose.yml` (not `ports:`). `expose`
makes a port reachable **only within the Docker network** — it is never bound on the host.
The frontend therefore cannot reach `http://localhost:8081/api/Problem/`; it must go through
`http://localhost/api/Problem/` (the gateway).

Evidence:
- `docker-compose.yml:17-18` — gateway `ports: "80:80"`
- `docker-compose.yml:10-11` — sql-server `ports: "1433:1433"`
- `docker-compose.yml:39-40` — anonymous-user-service `expose: "8080"` (same pattern for all 11)

---

## Upstream block rule

One `upstream` block per service, declared at `http {}` scope (before the `server {}` block).

```nginx
upstream <name>_service {
    server <container-name>:8080;
}
```

### Naming conventions (MUST match exactly)

| upstream name | Docker container name | Evidence (nginx.conf line) |
|---|---|---|
| `anonymous_service` | `anonymous-user-service` | `:17-19` |
| `organization_service` | `organization-service` | `:21-23` |
| `problem_service` | `problem-service` | `:25-27` |
| `problem_box_service` | `problem-box-service` | `:29-31` |
| `subscription_service` | `subscription-service` | `:33-35` |
| `suggestion_service` | `suggestion-service` | `:37-39` |
| `suggestion_box_service` | `suggestion-box-service` | `:41-43` |
| `billing_notification_service` | `billing-notification-service` | `:45-47` |
| `system_notification_service` | `system-notification-service` | `:49-51` |
| `attachment_service` | `attachment-service` | `:53-55` |
| `logger_service` | `logger-service` | `:57-59` |

Rules:
- Upstream name: `snake_case` with `_service` suffix.
- Container name (the `server` directive value): `kebab-case` matching the `container_name:` key
  in `docker-compose.yml`. These are Docker DNS names — they must match exactly.
- Port is always **8080** (Kestrel `ListenAnyIP(8080)` is hardcoded in every service's
  `Program.cs`; see `ProblemService/Program.cs:43-46`).

---

## Server-level proxy headers (set ONCE)

These four directives live at `server {}` scope (`gateway/nginx.conf:64-67`) and are inherited
by every `location` block automatically — do not repeat them inside a `location` block.

```nginx
proxy_set_header Host              $host;
proxy_set_header X-Real-IP         $remote_addr;
proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
proxy_set_header X-Forwarded-Proto $scheme;
```

`X-Forwarded-For` and `X-Forwarded-Proto` are consumed by the rate-limiter policies in
`AnonymousUserService` and `OrganizationService` for client-IP identification.
See also `.claude/rules/auth-jwt.md` for the rate-limit policy details.

---

## Location block template (copy-paste exactly)

```nginx
location /api/<Controller>/ {
    if ($request_method = 'OPTIONS') {
        add_header Access-Control-Allow-Origin  "*" always;
        add_header Access-Control-Allow-Methods "GET, POST, PUT, DELETE, OPTIONS" always;
        add_header Access-Control-Allow-Headers "Content-Type, Authorization" always;
        add_header Access-Control-Max-Age 3600;
        return 204;
    }
    add_header Access-Control-Allow-Origin  "*" always;
    add_header Access-Control-Allow-Methods "GET, POST, PUT, DELETE, OPTIONS" always;
    add_header Access-Control-Allow-Headers "Content-Type, Authorization" always;
    proxy_pass http://<upstream_name>/api/<Controller>/;
}
```

### What each part does

| Directive | Purpose |
|---|---|
| `if ($request_method = 'OPTIONS') { return 204; }` | Handles CORS preflight. Browser sends OPTIONS before every cross-origin mutation; without this short-circuit the preflight reaches the ASP.NET controller and returns a 405/404, aborting the actual request. |
| `Access-Control-Allow-Origin "*"` | Permits any origin (dev + prod convenience). Present inside the `if` block AND outside it — both paths must set headers. |
| `Access-Control-Allow-Methods "GET, POST, PUT, DELETE, OPTIONS"` | Exact string used in all 19 existing blocks; must include `OPTIONS` so browsers accept the preflight response. |
| `Access-Control-Allow-Headers "Content-Type, Authorization"` | `Authorization` is required for any authenticated endpoint (JWT bearer). |
| `Access-Control-Max-Age 3600` | Tells the browser to cache this preflight for 1 hour, reducing round-trips. Only set inside the `if` block. |
| `proxy_pass http://<upstream_name>/api/<Controller>/;` | Forward to the upstream. The trailing slash is mandatory — without it Nginx strips the prefix and the ASP.NET route won't match. |

Canonical example — `location /api/Problem/` (`gateway/nginx.conf:139-151`):

```nginx
location /api/Problem/ {
    if ($request_method = 'OPTIONS') {
        add_header Access-Control-Allow-Origin  "*" always;
        add_header Access-Control-Allow-Methods "GET, POST, PUT, DELETE, OPTIONS" always;
        add_header Access-Control-Allow-Headers "Content-Type, Authorization" always;
        add_header Access-Control-Max-Age 3600;
        return 204;
    }
    add_header Access-Control-Allow-Origin  "*" always;
    add_header Access-Control-Allow-Methods "GET, POST, PUT, DELETE, OPTIONS" always;
    add_header Access-Control-Allow-Headers "Content-Type, Authorization" always;
    proxy_pass http://problem_service/api/Problem/;
}
```

---

## Route prefix → upstream mapping (all 19 location blocks)

| Route prefix | Upstream | Service | nginx.conf lines |
|---|---|---|---|
| `/api/AnonymousUser/` | `anonymous_service` | AnonymousUserService | `:70-82` |
| `/api/BoxAccessLink/` | `anonymous_service` | AnonymousUserService | `:83-95` |
| `/api/Organization/` | `organization_service` | OrganizationService | `:98-110` |
| `/api/User/` | `organization_service` | OrganizationService | `:111-123` |
| `/api/UserRole/` | `organization_service` | OrganizationService | `:124-136` |
| `/api/Problem/` | `problem_service` | ProblemService | `:139-151` |
| `/api/ProblemCategory/` | `problem_service` | ProblemService | `:152-164` |
| `/api/ProblemComment/` | `problem_service` | ProblemService | `:165-177` |
| `/api/ProblemBox/` | `problem_box_service` | ProblemBoxService | `:180-192` |
| `/api/Subscription/` | `subscription_service` | SubscriptionService | `:195-207` |
| `/api/SubscriptionPlan/` | `subscription_service` | SubscriptionService | `:208-220` |
| `/api/Payment/` | `subscription_service` | SubscriptionService | `:221-233` |
| `/api/Suggestion/` | `suggestion_service` | SuggestionService | `:236-248` |
| `/api/SuggestionCategory/` | `suggestion_service` | SuggestionService | `:249-261` |
| `/api/SuggestionComment/` | `suggestion_service` | SuggestionService | `:262-274` |
| `/api/Vote/` | `suggestion_service` | SuggestionService | `:275-287` |
| `/api/SuggestionBox/` | `suggestion_box_service` | SuggestionBoxService | `:290-302` |
| `/api/BillingNotification/` | `billing_notification_service` | BillingNotificationService | `:305-317` |
| `/api/SystemNotification/` | `system_notification_service` | SystemNotificationService | `:320-332` |
| `/api/Attachment/` | `attachment_service` | AttachmentService | `:335-347` |
| `/api/Logger/` | `logger_service` | LoggerService | `:349-361` |
| `/api/Audit/` | `mcp_gateway_service` | McpGateway (T7) | `:372-388` |
| `/api/AiChat/` | `ai_assistant_service` | AiAssistantService (T7) | `:389-405` |
| `/health` | (inline `return 200`) | gateway self-check | `:364-377` |

Note: One service may serve multiple route prefixes (e.g. `problem_service` handles
`/api/Problem/`, `/api/ProblemCategory/`, `/api/ProblemComment/`). Each prefix needs its own
`location` block — there is no wildcard matching or shared block.

---

## Adding a new controller: step-by-step checklist

1. **Controller exists in the service** — `[Route("api/[controller]")]` on the class, giving
   `api/MyFeature` as the URL prefix.

2. **Determine target service** — which existing service owns this controller? Look up its
   upstream name from the table above.

3. **Add upstream block** (only if adding a brand-new service, not a new controller to an
   existing service):
   ```nginx
   upstream my_new_service {
       server my-new-service:8080;
   }
   ```
   Place inside `http {}`, before the `server {}` block.

4. **Add container to `docker-compose.yml`** (new service only):
   ```yaml
   my-new-service:
     build: ./MyNewService
     container_name: my-new-service
     expose:
       - "8080"
     environment:
       - ASPNETCORE_ENVIRONMENT=Development
       - ConnectionStrings__MyNewDB=Server=sql-server;Database=MyNewDB;...
   ```
   Use `expose:` not `ports:` — services must NOT be published on the host.

5. **Add `location` block** to `gateway/nginx.conf` inside `server {}`:
   ```nginx
   location /api/MyFeature/ {
       if ($request_method = 'OPTIONS') {
           add_header Access-Control-Allow-Origin  "*" always;
           add_header Access-Control-Allow-Methods "GET, POST, PUT, DELETE, OPTIONS" always;
           add_header Access-Control-Allow-Headers "Content-Type, Authorization" always;
           add_header Access-Control-Max-Age 3600;
           return 204;
       }
       add_header Access-Control-Allow-Origin  "*" always;
       add_header Access-Control-Allow-Methods "GET, POST, PUT, DELETE, OPTIONS" always;
       add_header Access-Control-Allow-Headers "Content-Type, Authorization" always;
       proxy_pass http://my_new_service/api/MyFeature/;
   }
   ```

6. **Rebuild / restart** — `docker-compose up -d --build gateway` (or the target service if it
   changed).

---

## WRONG / CORRECT

### WRONG: Adding a controller without a location block

```csharp
// MyNewService/Controllers/MyFeatureController.cs
[ApiController]
[Route("api/[controller]")]
public class MyFeatureController : ControllerBase { ... }
```

Without a `location /api/MyFeature/` block in `nginx.conf`, the gateway returns **404** for
every request to `/api/MyFeature/*`. The controller is unreachable from the frontend because
the gateway has no route for it. No error in the service logs — the request never arrives.

```nginx
// WRONG: nothing added to nginx.conf
// gateway returns 404 for all /api/MyFeature/* requests
```

CORRECT: add the full `location` block as shown in the template above.

---

### WRONG: Using `localhost` or a bare port as the upstream server

```nginx
# WRONG
upstream my_service {
    server localhost:5001;  # host loopback — unavailable inside a container
}

# ALSO WRONG
upstream my_service {
    server my-service;      # missing port — Nginx defaults to :80, not :8080
}

# ALSO WRONG
upstream my_service {
    server my-service:5001; # wrong port — Kestrel is pinned to 8080
}
```

CORRECT:

```nginx
upstream my_service {
    server my-service:8080;  # Docker container name, port 8080
}
```

`localhost` inside a Docker container resolves to the container itself, not the host.
Container-to-container communication uses the Docker network DNS name (`container_name:` value).
All services listen on **8080** only (Kestrel `ListenAnyIP(8080)`).

---

### WRONG: Omitting the OPTIONS branch (CORS preflight fails)

```nginx
# WRONG — missing the if-OPTIONS block
location /api/MyFeature/ {
    add_header Access-Control-Allow-Origin  "*" always;
    add_header Access-Control-Allow-Methods "GET, POST, PUT, DELETE, OPTIONS" always;
    add_header Access-Control-Allow-Headers "Content-Type, Authorization" always;
    proxy_pass http://my_service/api/MyFeature/;
}
```

Without `if ($request_method = 'OPTIONS') { return 204; }`, CORS preflight requests (OPTIONS)
are forwarded to the ASP.NET controller. The controller has no OPTIONS handler, returns 405 or
404, and the browser aborts the real request with a CORS error. Symptom: all cross-origin
mutations (POST/PUT/DELETE) fail in the browser even though the endpoint works via curl.

CORRECT: always include the `if-OPTIONS` block as shown in the template.

---

### WRONG: Missing the trailing slash on `proxy_pass`

```nginx
# WRONG
proxy_pass http://problem_service/api/Problem;   # no trailing slash

# WRONG
proxy_pass http://problem_service;               # no path at all
```

Without a trailing slash Nginx may append the full original URI, producing double-prefixed
paths like `/api/Problem/api/Problem/1`. Always use a trailing slash:

```nginx
proxy_pass http://problem_service/api/Problem/;
```

---

## GOTCHA: CORS preamble is fully duplicated in every location block

There is no `map {}` block, no `geo {}`, no shared include for CORS headers. The identical
8-line CORS preamble (4 lines inside `if-OPTIONS` + 3 headers outside it) is copy-pasted into
all 19 `location` blocks (plus `/health`).

This is intentional (or at least deliberate inertia) — do not "fix" it by extracting a shared
include unless you are also prepared to test every route. The duplication is load-bearing: Nginx
evaluates `add_header` directives per-block, and any refactoring that moves them to a parent
scope or an included file will change inheritance behavior.

**Implication for new blocks:** when adding a `location` block, copy the full preamble from an
existing block. Do not abbreviate it. Any deviation from the standard 3-header set risks
breaking specific frontends or future features.

---

## GOTCHA: internal-only channels get NO location block (expose only JWT-protected surfaces)

A service may have an endpoint that must NOT be reachable from outside the Docker network. The
McpGateway's MCP tool channel (`MapMcp("/mcp")`) is the trust-boundary entry for AI agents — it is
deliberately given **no** nginx `location` block, so only the internal `ai-assistant-service` (same
Docker network, calls `http://mcp-gateway:8080/mcp`) can reach it. Only the JWT-protected REST
surfaces are exposed: `location /api/Audit/` → `mcp_gateway_service`, and the agent's own
`location /api/AiChat/` → `ai_assistant_service`.

| Rule | Detail |
|---|---|
| Expose the REST surface, not the raw channel | `/api/Audit/` + `/api/AiChat/` get blocks; `/mcp` does not |
| `expose: "8080"` (not `ports:`) keeps it internal | the gateway container is never published on the host — reachable only via other containers |
| Upstream `_service` suffix even when container name lacks it | `upstream mcp_gateway_service { server mcp-gateway:8080; }` — snake_case label → kebab container DNS |

Evidence: `gateway/nginx.conf:61-67` (upstreams), `:372-405` (Audit/AiChat blocks, no `/mcp`);
`McpGateway/Program.cs` (`MapMcp("/mcp").RequireAuthorization()`); `docker-compose.yml:195,230`.

---

## Key facts for cross-references

| Fact | Where to look |
|---|---|
| Kestrel `ListenAnyIP(8080)` — why all upstreams use `:8080` | `ProblemService/Program.cs:43-46` |
| `X-Forwarded-For` consumed by rate limiter | `AnonymousUserService/Program.cs:30-62`, `OrganizationService/Program.cs:32-64` — also see `.claude/rules/auth-jwt.md` |
| Services don't do their own CORS — gateway handles it entirely | `gateway/nginx.conf:64-377` (no `UseCors` in any `Program.cs`) |
| Container names are the authoritative DNS names in Docker | `docker-compose.yml:container_name:` fields |
