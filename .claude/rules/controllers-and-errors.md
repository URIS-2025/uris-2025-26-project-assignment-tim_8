# Rule: Controllers and Error Handling

Scope: every `Controllers/*.cs` file across all 11 services.  
Evidence base: 18 controller files, 106 `TryLogAsync` calls, 52+ `new { error }` returns.

---

## 1. Required Class-Level Attributes

Every controller **must** have both attributes on the class — no exceptions, no service is exempt.

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProblemController : Controller
```

| Attribute | Purpose |
|---|---|
| `[ApiController]` | Enables automatic model-state 400, binding-source inference |
| `[Route("api/[controller]")]` | Matches the Nginx `location /api/Problem/` upstream block |

Evidence: `ProblemService/Controllers/ProblemController.cs:10-11`, `OrganizationService/Controllers/UserController.cs:13-14`, `SuggestionService/Controllers/SuggestionController.cs:10-11`.

---

## 2. Constructor Injection

Inject exactly these three dependencies (IMapper is optional for write-only controllers):

```csharp
private readonly IProblemRepository _problemRepository;
private readonly IMapper _mapper;
private readonly LoggerServiceClient _loggerClient;

public ProblemController(
    IProblemRepository problemRepository,
    IMapper mapper,
    LoggerServiceClient loggerClient)
{
    _problemRepository = problemRepository;
    _mapper = mapper;
    _loggerClient = loggerClient;
}
```

| Dependency | Type | Registration |
|---|---|---|
| Repository | `I<Entity>Repository` (scoped) | `builder.Services.AddScoped<IProblemRepository, ProblemRepository>()` |
| Mapper | `IMapper` | `builder.Services.AddAutoMapper(typeof(Program).Assembly)` |
| Logger client | `LoggerServiceClient` (singleton) | `builder.Services.AddHttpClient("LoggerService", ...)` |

Evidence: `ProblemService/Controllers/ProblemController.cs:14-23`, `OrganizationService/Controllers/UserController.cs:17-24`, `SuggestionService/Controllers/SuggestionController.cs:14-23`.

Note: `UserController` (OrganizationService) omits `IMapper` because it delegates mapping to the repository layer. That is acceptable — do not inject mapper if the controller does not use it.

---

## 3. Action Signature Rules

| Return scenario | Correct signature |
|---|---|
| Returns a single DTO | `public ActionResult<ProblemDTO> GetProblemById(Guid id)` |
| Returns a collection | `public ActionResult<IEnumerable<ProblemDTO>> GetAllProblems()` |
| Creates a resource | `public async Task<ActionResult<ProblemCreatedDTO>> CreateProblem([FromBody] ProblemCreationDTO problem)` |
| Deletes (no body) | `public async Task<IActionResult> DeleteProblem(Guid id)` |

Rules:
- Read-only actions that cannot fail are **not** `async` — no `Task<>` wrapper needed.
- Mutating actions (POST/PUT/DELETE) that call `TryLogAsync` **must** be `async Task<…>`.
- Always use `[FromBody]` on POST/PUT parameters; `[FromRoute]` is inferred from `{id}` in the route template.

---

## 4. Result Helpers — Canonical Set

| Situation | Return expression |
|---|---|
| Resource created | `return Created("", result);` |
| Read / update success | `return Ok(result);` |
| Delete success | `return NoContent();` |
| Not found (catch `KeyNotFoundException`) | `return NotFound(new { error = ex.Message });` |
| Bad input (catch `Exception`) | `return BadRequest(new { error = ex.Message });` |
| Duplicate / conflict (catch `InvalidOperationException`) | `return Conflict(new { error = ex.Message });` |
| Authentication failure | `return Unauthorized(new { error = ex.Message });` |

Evidence: `ProblemService/Controllers/ProblemController.cs:64,100,114,136,151`, `OrganizationService/Controllers/UserController.cs:61,104,118,156,192`.

### WRONG / CORRECT

```csharp
// WRONG — returns the raw entity, not the DTO
return Created("", problemEntity);

// CORRECT — result is always a DTO (mapped in the repository layer)
return Created("", result);  // result is ProblemCreatedDTO
```

```csharp
// WRONG — bare 200 on create
return Ok(result);

// CORRECT — 201 signals creation to the caller
return Created("", result);
```

---

## 5. The Audit-Log Pattern (TryLogAsync)

Every mutating action (POST, PUT, DELETE) **must** call `_loggerClient.TryLogAsync` on **both** the success path and the catch path. Read-only GET actions do not require a log call.

### LogCreationDTO field reference

Canonical source: `LoggerService/Models/DTOs/LogCreationDTO.cs`  
Local copies used at call sites: `*/Clients/LogCreationDTO.cs` (10 copies, one per non-logger service).

| Field | Type | Required? | Success value | Failure value |
|---|---|---|---|---|
| `UserId` | `string?` | no | `User.Identity?.Name` | `User.Identity?.Name` |
| `Action` | `string` | yes | `"CREATE_PROBLEM"` (VERB_ENTITY) | same verb |
| `EntityName` | `string?` | no | `"Problem"` | `"Problem"` |
| `NewValues` | `string?` | no | `JsonSerializer.Serialize(result)` | omit / null |
| `OldValues` | `string?` | no | old DTO before update, or `id.ToString()` | `id.ToString()` on delete |
| `IsSuccess` | `bool` | yes | `true` | `false` |
| `ServiceName` | `string` | yes | `"ProblemService"` | same |
| `HttpMethod` | `string?` | no | `"POST"` / `"PUT"` / `"DELETE"` | same |

The call also passes two extra arguments (not part of the DTO struct):
- `Request.Headers["Authorization"]` — bearer token forwarded to LoggerService
- `HttpContext.RequestAborted` — cancellation token

### Canonical success + failure call pair

```csharp
[HttpPost]
public async Task<ActionResult<ProblemCreatedDTO>> CreateProblem(
    [FromBody] ProblemCreationDTO problem)
{
    try
    {
        var result = _problemRepository.CreateProblem(problem);

        await _loggerClient.TryLogAsync(new LogCreationDTO
        {
            UserId      = User.Identity?.Name,
            Action      = "CREATE_PROBLEM",
            EntityName  = "Problem",
            NewValues   = JsonSerializer.Serialize(result),
            IsSuccess   = true,
            ServiceName = "ProblemService",
            HttpMethod  = "POST"
        }, Request.Headers["Authorization"], HttpContext.RequestAborted);

        return Created("", result);
    }
    catch (Exception ex)
    {
        await _loggerClient.TryLogAsync(new LogCreationDTO
        {
            UserId      = User.Identity?.Name,
            Action      = "CREATE_PROBLEM",
            EntityName  = "Problem",
            IsSuccess   = false,
            ServiceName = "ProblemService",
            HttpMethod  = "POST"
        }, Request.Headers["Authorization"], HttpContext.RequestAborted);

        return BadRequest(new { error = ex.Message });
    }
}
```

Source: `ProblemService/Controllers/ProblemController.cs:46-80` (verbatim).

### WRONG / CORRECT

```csharp
// WRONG — logs only on success; failure path is silent
try
{
    var result = _problemRepository.CreateProblem(problem);
    await _loggerClient.TryLogAsync(...IsSuccess = true...);
    return Created("", result);
}
catch (Exception ex)
{
    return BadRequest(new { error = ex.Message }); // no audit log!
}

// CORRECT — audit log in BOTH branches (see canonical pair above)
```

```csharp
// WRONG — re-throws, bypasses the error-shape contract
catch (Exception ex)
{
    throw; // becomes 500 with { message } from global handler, not { error }
}

// CORRECT — catch, log, return shaped error
catch (Exception ex)
{
    await _loggerClient.TryLogAsync(...IsSuccess = false...);
    return BadRequest(new { error = ex.Message });
}
```

---

## 6. Exception-Type Routing

| Exception type | Meaning | Response |
|---|---|---|
| `KeyNotFoundException` | Entity with given ID does not exist | `NotFound(new { error = ex.Message })` |
| `ArgumentException` | Invalid argument (validation in repo) | `BadRequest(new { error = ex.Message })` |
| `InvalidOperationException` | Duplicate / conflict state | `Conflict(new { error = ex.Message })` |
| `UnauthorizedAccessException` | Auth failure (e.g. bad refresh token) | `Unauthorized(new { error = ex.Message })` |
| `Exception` (base) | Anything else — last-resort catch | `BadRequest(new { error = ex.Message })` |

Ordering: specific exceptions before `Exception`. Example from `ProblemController.UpdateProblem`:

```csharp
catch (KeyNotFoundException ex)
{
    // ... TryLogAsync IsSuccess=false ...
    return NotFound(new { error = ex.Message });
}
// no base Exception catch here — falls through to global handler
```

Source: `ProblemService/Controllers/ProblemController.cs:102-115`, `OrganizationService/Controllers/UserController.cs:63-80`.

---

## 7. Global Exception Handler vs Controller Catch — KEY INCONSISTENCY

> **GOTCHA — known inconsistency. Do not "fix" it. Know it exists.**

| Layer | JSON key | Status | Source |
|---|---|---|---|
| Controller `catch` blocks | `error` | 400 / 404 / 401 / 409 | `ProblemService/Controllers/ProblemController.cs:78,114,151` |
| Rate-limiter rejection | `error` | 429 | `AnonymousUserService/Program.cs:60` |
| `LoggerController` extra field | `error` + `detail` | 400 | `LoggerService/Controllers/LoggerController.cs:71` |
| Global `UseExceptionHandler` | `message` | 500 | `ProblemService/Program.cs:70-85` |

Frontend code cannot rely on a single key — must check both `error` and `message`. When adding new error handling, use `new { error = ... }` to stay consistent with the 52-occurrence majority; the global handler's `message` key is **not** the pattern for controller code.

Global handler (verbatim, `ProblemService/Program.cs:70-85`):

```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var error = context.Features.Get<IExceptionHandlerFeature>();
        if (error != null)
        {
            await context.Response.WriteAsJsonAsync(new
            {
                message = error.Error.Message   // <-- "message", not "error"
            });
        }
    });
});
```

---

## 8. Summary Checklist

When adding a new controller action:

- [ ] Class has `[ApiController]` + `[Route("api/[controller]")]`
- [ ] Constructor injects `I<X>Repository`, `IMapper` (if used), `LoggerServiceClient`
- [ ] GET actions return `ActionResult<T>` or `ActionResult<IEnumerable<T>>` — no `async` unless needed
- [ ] POST/PUT/DELETE are `async Task<ActionResult<T>>` or `async Task<IActionResult>`
- [ ] POST returns `Created("", result)`, not `Ok(result)`
- [ ] DELETE returns `NoContent()` on success
- [ ] `TryLogAsync` is called on the success path (inside `try`) with `IsSuccess = true`
- [ ] `TryLogAsync` is called on the failure path (inside `catch`) with `IsSuccess = false`
- [ ] `NewValues = JsonSerializer.Serialize(result)` on success; omit on failure
- [ ] `OldValues` set for UPDATE (serialize old state before overwrite) and DELETE (id string)
- [ ] `KeyNotFoundException` → `NotFound(new { error })`, before base `Exception` catch
- [ ] Base `Exception` catch uses `new { error = ex.Message }`, not `throw`
- [ ] No raw entity returned — always a DTO
