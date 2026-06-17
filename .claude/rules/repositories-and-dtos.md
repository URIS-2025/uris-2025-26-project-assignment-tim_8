# Rule: Repositories and DTOs

Data access is **always** handled by a repository. Controllers never touch `DbContext` directly
and never return entity types. Every public method in a repository returns a DTO (or `void` /
`bool`), never an EF entity.

---

## 1. Interface + Implementation pair

Each aggregate has an interface and a concrete class, both under `<Service>/Data/`.

| File | Purpose |
|---|---|
| `Data/I<Entity>Repository.cs` | Contract only — no logic, no state |
| `Data/<Entity>Repository.cs` | Implements the interface; owns `DbContext` + `IMapper` |

**Real examples**

| Interface | Implementation | Service |
|---|---|---|
| `Data/IProblemRepository.cs` | `Data/ProblemRepository.cs` | ProblemService |
| `Data/IAnonymousUserRepository.cs` | `Data/AnonymousUserRepository.cs` | AnonymousUserService |
| `Data/IBillingNotificationRepository.cs` | `Data/BillingNotificationRepository.cs` | BillingNotificationService |
| `Data/IAttachmentRepository.cs` | `Data/AttachmentRepository.cs` | AttachmentService |

The interface declares only DTO-typed signatures:

```csharp
// ProblemService/Data/IProblemRepository.cs
public interface IProblemRepository
{
    IEnumerable<ProblemDTO> GetAllProblems();
    ProblemDTO GetProblemById(Guid id);
    ProblemCreatedDTO CreateProblem(ProblemCreationDTO problem);
    ProblemDTO UpdateProblem(ProblemUpdateDTO problem);
    void DeleteProblem(Guid id);
    // NOTE: SaveChanges() is NOT in the interface — it is a private helper on the impl.
}
```

### Constructor injection

Every repository takes `<Service>Context` and `IMapper` as constructor parameters. Some also
take `IConfiguration` (e.g. `AnonymousUserRepository` for JWT key material).

```csharp
// CORRECT
public ProblemRepository(ProblemContext context, IMapper mapper)
{
    _context = context;
    _mapper = mapper;
}

// AnonymousUserRepository also injects IConfiguration for JWT:
public AnonymousUserRepository(AnonymousUserContext context, IMapper mapper, IConfiguration configuration)
```

---

## 2. `SaveChanges()` helper

Every repository exposes `public bool SaveChanges()` using the identical one-liner:

```csharp
// CORRECT — one-liner form (preferred):
public bool SaveChanges() => _context.SaveChanges() > 0;

// Also acceptable — block form (older repos):
public bool SaveChanges()
{
    return _context.SaveChanges() > 0;
}
```

**Evidence (20 repo files, sample)**

| File | Line |
|---|---|
| `ProblemService/Data/ProblemRepository.cs` | :19–22 |
| `AnonymousUserService/Data/AnonymousUserRepository.cs` | :26 |
| `OrganizationService/Data/UserRepository.cs` | :27 |
| `BillingNotificationService/Data/BillingNotificationRepository.cs` | :20–22 |
| `AttachmentService/Data/AttachmentRepository.cs` | :22–24 |
| `AnonymousUserService/Data/BoxAccessLinkRepository.cs` | :18–20 |

The method is called internally after every mutating operation:

```csharp
_context.Problems.Add(entity);
SaveChanges();
```

**WRONG / CORRECT**

| WRONG | CORRECT |
|---|---|
| `_context.SaveChanges();` (void, result ignored) | `SaveChanges();` (calls the bool helper) |
| `if (_context.SaveChanges() > 0)` inline in method body | `SaveChanges()` helper + trust the throw path |
| Return `true`/`false` from Create to the controller | Return a DTO; let a thrown exception signal failure |

---

## 3. Entity-to-DTO mapping via `IMapper`

All mapping is done with AutoMapper. Two styles appear in the codebase:

### Style A — `foreach` loop (ProblemRepository, many others)

```csharp
// ProblemService/Data/ProblemRepository.cs:27-33
var problems = _context.Problems.ToList();
var problemResult = new List<ProblemDTO>();
foreach (var problem in problems)
{
    var dto = _mapper.Map<ProblemDTO>(problem);
    problemResult.Add(dto);
}
return problemResult;
```

### Style B — `.Select()` one-liner (AnonymousUserRepository)

```csharp
// AnonymousUserService/Data/AnonymousUserRepository.cs:40
return _context.AnonymousUsers.ToList().Select(u => _mapper.Map<AnonymousUserDTO>(u));
```

### Style C — direct collection mapping (AttachmentRepository, newer)

```csharp
// AttachmentService/Data/AttachmentRepository.cs:30
return _mapper.Map<IEnumerable<AttachmentDTO>>(attachments);
```

Style C is the most concise. Prefer it for new code.

**Evidence — `_mapper.Map` in Data/ files (sample)**

| File | Line | Direction |
|---|---|---|
| `ProblemService/Data/ProblemRepository.cs` | :30, :41, :51, :66, :71, :88 | entity→DTO and DTO→entity (on create) |
| `AnonymousUserService/Data/AnonymousUserRepository.cs` | :40, :48, :58, :66 | entity→DTO and DTO→entity |
| `BillingNotificationService/Data/BillingNotificationRepository.cs` | :27, :30, :50, :64, :72, :75 | entity↔DTO |
| `AttachmentService/Data/AttachmentRepository.cs` | :30, :36, :44, :52, :57, :61 | entity↔DTO |
| `OrganizationService/Data/UserRoleRepository.cs` | :24, :28, :49, :62, :71, :73 | entity↔DTO |

**WRONG / CORRECT**

| WRONG | CORRECT |
|---|---|
| `new ProblemDTO { Title = entity.Title, ... }` (manual prop copy) | `_mapper.Map<ProblemDTO>(entity)` |
| Return `entity` from a public method | Map to DTO first, then return |
| Map inside the controller action | Map inside the repository |

---

## 4. `Id` and `CreatedAt` set in the repository on create

Never set `Id` or `CreatedAt` in the controller or in the DTO. Set them in the repo, after mapping the input DTO to the entity.

```csharp
// CORRECT — ProblemService/Data/ProblemRepository.cs:66-68
var entity = _mapper.Map<Problem>(problem);
entity.Id = Guid.NewGuid();
entity.CreatedAt = DateTime.UtcNow;
_context.Problems.Add(entity);
SaveChanges();
return _mapper.Map<ProblemCreatedDTO>(entity);
```

```csharp
// CORRECT — AnonymousUserService/Data/AnonymousUserRepository.cs:58-62
var entity = _mapper.Map<AnonymousUser>(user)!;
entity.Id = Guid.NewGuid();
entity.CreatedAt = DateTime.UtcNow;
entity.Username = normalizedUsername;
```

**Evidence**

| File | Lines |
|---|---|
| `ProblemService/Data/ProblemRepository.cs` | :67–68 |
| `AnonymousUserService/Data/AnonymousUserRepository.cs` | :59–60 |
| `AnonymousUserService/Data/BoxAccessLinkRepository.cs` | (same pattern, `entity.Id = Guid.NewGuid()`) |

**WRONG / CORRECT**

| WRONG | CORRECT |
|---|---|
| `var entity = new Problem { Id = Guid.NewGuid(), ... }` in the controller | `entity.Id = Guid.NewGuid()` in the repository after `_mapper.Map` |
| `CreatedAt = DateTime.Now` | `CreatedAt = DateTime.UtcNow` (always UTC) |
| DTO has an `Id` field that the client supplies | `Id` is always server-generated in the repository |

---

## 5. Validation and exception throwing inside the repository

Input validation and "not found" checks belong in the repository, not the controller. The
controller's `catch` block converts exceptions to HTTP error responses.

### Exception types in use

| Scenario | Exception type | Example repo |
|---|---|---|
| Required field missing / bad value | `ArgumentException` | ProblemRepository |
| Record not found (many repos) | `ArgumentException` | ProblemRepository |
| Record not found (AnonymousUserService) | `KeyNotFoundException` | AnonymousUserRepository |
| Duplicate / business rule violation | `InvalidOperationException` | AnonymousUserRepository (`"Username is already taken."`) |
| Auth failure | `UnauthorizedAccessException` | AnonymousUserRepository (Login, RefreshToken) |

> **GOTCHA — inconsistent "not found" exception type:**
> `ProblemRepository` throws `ArgumentException` for missing records
> (`ProblemService/Data/ProblemRepository.cs:40, 77, 95`), while
> `AnonymousUserRepository` throws `KeyNotFoundException`
> (`AnonymousUserService/Data/AnonymousUserRepository.cs:32`).
> Both are caught by the controller's generic `catch (Exception ex)` block and turned into
> `BadRequest(new { error = ex.Message })`. Do not rely on the specific type at the call site —
> the controller does not differentiate.

**Validation pattern (ProblemRepository)**

```csharp
// ProblemService/Data/ProblemRepository.cs:59-64
if (problem.ProblemBoxId == Guid.Empty)
    throw new ArgumentException("ProblemBoxId must be provided.");
if (string.IsNullOrWhiteSpace(problem.Title))
    throw new ArgumentException("Title must be provided.");
if (string.IsNullOrWhiteSpace(problem.Description))
    throw new ArgumentException("Description must be provided.");
```

**Not-found pattern (two styles)**

```csharp
// Style 1 — ArgumentException (ProblemRepository, more common)
// ProblemService/Data/ProblemRepository.cs:39-40
if (problem == null)
    throw new ArgumentException("Problem with that Id does not exist.");

// Style 2 — KeyNotFoundException (AnonymousUserRepository)
// AnonymousUserService/Data/AnonymousUserRepository.cs:31-32
if (user == null)
    throw new KeyNotFoundException($"Anonymous user with id {id} not found.");
```

**Duplicate check (InvalidOperationException)**

```csharp
// AnonymousUserService/Data/AnonymousUserRepository.cs:55-56
if (_context.AnonymousUsers.Any(u => u.Username == normalizedUsername))
    throw new InvalidOperationException("Username is already taken.");
```

**WRONG / CORRECT**

| WRONG | CORRECT |
|---|---|
| Return `null` from a Get method when not found | Throw `ArgumentException` (or `KeyNotFoundException`) with descriptive message |
| Validate in the controller before calling the repo | Validate in the repository; controller just calls and catches |
| `throw new Exception(...)` generic | Use `ArgumentException`, `InvalidOperationException`, `KeyNotFoundException` as appropriate |

---

## 6. DTO / VO naming convention

All DTO and VO classes live under `<Service>/Models/DTOs/`. Use these suffixes consistently:

| Suffix | Role | When used | Real example |
|---|---|---|---|
| `XxxCreationDTO` | Create input (client → controller → repo) | POST body | `ProblemCreationDTO`, `AnonymousUserCreationDTO`, `BillingNotificationCreationDTO` |
| `XxxCreatedDTO` | Create response (repo → controller → client) | POST 200/201 body | `ProblemCreatedDTO`, `UserRoleCreatedDTO`, `ProblemBoxCreatedDTO` |
| `XxxUpdateDTO` | Update input (client → controller → repo) | PUT body | `ProblemUpdateDTO`, `ProblemCategoryUpdateDTO`, `ProblemCommentUpdateDTO` |
| `XxxDTO` | Read / list response | GET body | `ProblemDTO`, `AnonymousUserDTO`, `BillingNotificationDTO`, `UserRoleDTO` |
| `XxxVO` | Value object fetched from **another service** | Embedded in a DTO | `AttachmentVO` (fetched from AttachmentService, embedded in ProblemDTO), `ProblemCommentAuthorUserVO` (fetched from OrganizationService) |

**Concrete file list — `ProblemService/Models/DTOs/`**

```
ProblemCreationDTO.cs          — POST /api/Problem input
ProblemCreatedDTO.cs           — POST /api/Problem response
ProblemUpdateDTO.cs            — PUT /api/Problem input
ProblemDTO.cs                  — GET /api/Problem response
ProblemCategoryCreationDTO.cs
ProblemCategoryCreatedDTO.cs
ProblemCategoryUpdateDTO.cs
ProblemCategoryDTO.cs
ProblemCommentCreationDTO.cs
ProblemCommentCreatedDTO.cs
ProblemCommentUpdateDTO.cs
ProblemCommentDTO.cs
AttachmentVO.cs                — value object from AttachmentService (not persisted locally)
ProblemCommentAuthorUserVO.cs  — value object from OrganizationService
```

**VO structure example**

```csharp
// ProblemService/Models/DTOs/AttachmentVO.cs
public class AttachmentVO
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public string FileType { get; set; }
    public string Url { get; set; }
    public DateTime UploadedAt { get; set; }
}
```

**WRONG / CORRECT**

| WRONG | CORRECT |
|---|---|
| `ProblemInputDTO` / `ProblemOutputDTO` | `ProblemCreationDTO` / `ProblemDTO` |
| `ProblemResponseDTO` | `ProblemCreatedDTO` (for create) or `ProblemDTO` (for reads) |
| Reuse `ProblemDTO` as update input | Define a dedicated `ProblemUpdateDTO` |
| Call a cross-service model a "DTO" | Suffix with `VO` (e.g. `AttachmentVO`) to signal it comes from another service |

---

## 7. AutoMapper registration and Profiles

### Registration (every `Program.cs`)

```csharp
// CORRECT — preferred (10 of 11 services)
builder.Services.AddAutoMapper(config => config.AddMaps(typeof(Program).Assembly));

// Also seen (SuggestionBoxService/Program.cs:25) — acceptable but less explicit:
builder.Services.AddAutoMapper(typeof(Program).Assembly);
```

**Evidence**

| File | Line |
|---|---|
| `ProblemService/Program.cs` | :16 |
| `AnonymousUserService/Program.cs` | :18 |
| `OrganizationService/Program.cs` | :17 |
| `BillingNotificationService/Program.cs` | :13 |
| `AttachmentService/Program.cs` | :14 |

### Profile location

Profiles live in `<Service>/Profiles/*.cs`. Each aggregate has its own Profile file.

**Profiles in ProblemService**

| File | Maps |
|---|---|
| `ProblemService/Profiles/ProblemProfile.cs` | `Problem ↔ ProblemDTO`, `↔ ProblemCreatedDTO`, `↔ ProblemCreationDTO`, `↔ ProblemUpdateDTO` |
| `ProblemService/Profiles/ProblemCategoryProfile.cs` | ProblemCategory entity ↔ category DTOs |
| `ProblemService/Profiles/ProblemCommentProfile.cs` | ProblemComment entity ↔ comment DTOs |

**Profile file structure**

```csharp
// ProblemService/Profiles/ProblemProfile.cs
public class ProblemProfile : Profile
{
    public ProblemProfile()
    {
        CreateMap<Problem, ProblemDTO>().ReverseMap();
        CreateMap<Problem, ProblemCreatedDTO>().ReverseMap();
        CreateMap<Problem, ProblemCreationDTO>().ReverseMap();
        CreateMap<Problem, ProblemUpdateDTO>().ReverseMap();
    }
}
```

All profiles discovered automatically by `AddMaps(typeof(Program).Assembly)` — no manual
registration of individual Profile classes.

**Cross-service profile count: 21 Profile files** across 11 services
(e.g. `OrganizationService/Profiles/UserProfile.cs`, `UserRoleProfile.cs`, `OrganizationProfile.cs`).

---

## 8. DI registration (scoped lifetime)

Every `I<Entity>Repository` is registered as `AddScoped` (one instance per HTTP request):

```csharp
// ProblemService/Program.cs:12-14
builder.Services.AddScoped<IProblemRepository, ProblemRepository>();
builder.Services.AddScoped<IProblemCommentRepository, ProblemCommentRepository>();
builder.Services.AddScoped<IProblemCategoryRepository, ProblemCategoryRepository>();

// AnonymousUserService/Program.cs:16-17
builder.Services.AddScoped<IAnonymousUserRepository, AnonymousUserRepository>();
builder.Services.AddScoped<IBoxAccessLinkRepository, BoxAccessLinkRepository>();

// OrganizationService/Program.cs:19-21
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
```

**WRONG / CORRECT**

| WRONG | CORRECT |
|---|---|
| `AddTransient<IXxxRepository, XxxRepository>()` | `AddScoped<IXxxRepository, XxxRepository>()` |
| `AddSingleton<IXxxRepository, XxxRepository>()` (EF DbContext is scoped — a singleton repo would capture a scoped context) | `AddScoped` |
| Instantiating `new XxxRepository(...)` in the controller | Constructor-inject `IXxxRepository` |

---

## 9. Complete WRONG / CORRECT summary

| # | WRONG | CORRECT | Evidence |
|---|---|---|---|
| W1 | Controller returns `entity` from EF | Return `_mapper.Map<XxxDTO>(entity)` | `ProblemService/Data/ProblemRepository.cs:41,71,88` |
| W2 | Controller calls `_context.Problems.FirstOrDefault(...)` directly | All data access through `IXxxRepository` | `ProblemService/Data/ProblemRepository.cs:38` (repo does it) |
| W3 | `new XxxDTO { ... }` manual property copy | `_mapper.Map<XxxDTO>(entity)` | `AnonymousUserService/Data/AnonymousUserRepository.cs:40,48` |
| W4 | `entity.Id = ...` set by controller or client | `entity.Id = Guid.NewGuid()` in repo on Create | `ProblemService/Data/ProblemRepository.cs:67` |
| W5 | `entity.CreatedAt = DateTime.Now` | `entity.CreatedAt = DateTime.UtcNow` | `AnonymousUserService/Data/AnonymousUserRepository.cs:60` |
| W6 | Return `null` when record not found | Throw `ArgumentException` or `KeyNotFoundException` | `ProblemService/Data/ProblemRepository.cs:40`; `AnonymousUserService/Data/AnonymousUserRepository.cs:32` |
| W7 | Validate input in controller before calling repo | Validate inside the repo method; throw `ArgumentException` | `ProblemService/Data/ProblemRepository.cs:59-64` |
| W8 | `AddSingleton` or `AddTransient` for repo | `AddScoped<IXxxRepository, XxxRepository>()` | `ProblemService/Program.cs:12-14` |
| W9 | Create `XxxProfile : Profile` and register manually | Inherit `Profile`, `AddMaps(typeof(Program).Assembly)` auto-discovers | `ProblemService/Program.cs:16` + `ProblemService/Profiles/ProblemProfile.cs` |
| W10 | Name cross-service shapes `AttachmentDTO` | Name them `AttachmentVO` (VO = value object, sourced externally) | `ProblemService/Models/DTOs/AttachmentVO.cs` |
