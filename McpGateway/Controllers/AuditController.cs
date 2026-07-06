using System.Security.Claims;
using McpGateway.Audit;
using McpGateway.Authorization;
using McpGateway.Context;
using McpGateway.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace McpGateway.Controllers;

/// <summary>
/// Read-only audit review endpoint (the audit dashboard's backend, Faza C). Admin sees all records;
/// a Manager is FORCED to their own org (a client-supplied organizationId filter is ignored) and
/// never sees null-org records. Returns a scrubbed <see cref="AuditDTO"/> page, never the entity.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")]
public class AuditController : ControllerBase
{
    private readonly IDbContextFactory<AuditDbContext> _factory;

    public AuditController(IDbContextFactory<AuditDbContext> factory) => _factory = factory;

    private const int MaxPageSize = 100;

    [HttpGet]
    public async Task<ActionResult<AuditPageDTO>> GetAudit(
        [FromQuery] string? agentId = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? toolName = null,
        [FromQuery] AuthDecision? decision = null,
        [FromQuery] AuditOutcome? outcome = null,
        [FromQuery] Guid? organizationId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        // Force-org: an admin may target one org (or all); a MANAGER is pinned to their own org from
        // the token — the client-supplied organizationId is ignored, so the LLM/UI can't widen scope.
        // "Admin" is matched case-insensitively against the SAME constant the tool path uses
        // (ToolAuthorizer.AdminRole), so the two never disagree on who is an admin.
        var isAdmin = User.FindAll(ClaimTypes.Role)
            .Any(c => string.Equals(c.Value, ToolAuthorizer.AdminRole, StringComparison.OrdinalIgnoreCase));

        Guid? orgFilter;
        if (isAdmin)
        {
            orgFilter = organizationId;
        }
        else
        {
            var ownOrg = User.FindFirst("OrganizationId")?.Value;
            if (!Guid.TryParse(ownOrg, out var mgrOrg))
                return Forbid(); // a manager without an org can't be safely scoped
            orgFilter = mgrOrg;
        }

        page = Math.Max(page, 0);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        // Compute the skip in long to avoid an int overflow for a huge page (which would wrap negative
        // and silently return page 0); bound it to int.MaxValue.
        var skip = (int)Math.Min((long)page * pageSize, int.MaxValue);

        await using var db = await _factory.CreateDbContextAsync(ct);
        var query = db.AuditEntries.AsQueryable();

        // orgFilter == null (admin, no org arg) → all orgs including null-org records.
        if (orgFilter is Guid org) query = query.Where(e => e.OrganizationId == org);
        if (!string.IsNullOrWhiteSpace(agentId)) query = query.Where(e => e.AgentId == agentId);
        if (!string.IsNullOrWhiteSpace(userId)) query = query.Where(e => e.UserId == userId);
        if (!string.IsNullOrWhiteSpace(toolName)) query = query.Where(e => e.ToolName == toolName);
        if (decision is AuthDecision d) query = query.Where(e => e.Decision == d);
        if (outcome is AuditOutcome o) query = query.Where(e => e.Outcome == o);
        // Timestamps are stored as UTC; treat an unspecified-kind query bound as UTC too so the
        // window isn't shifted by the caller's local offset (documented UTC contract).
        if (from is DateTime f) { var fu = DateTime.SpecifyKind(f, DateTimeKind.Utc); query = query.Where(e => e.Timestamp >= fu); }
        if (to is DateTime t) { var tu = DateTime.SpecifyKind(t, DateTimeKind.Utc); query = query.Where(e => e.Timestamp <= tu); }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip(skip)
            .Take(pageSize)
            .Select(e => new AuditDTO(
                e.Id, e.Timestamp, e.AgentId, e.UserId, e.UserRole, e.OrganizationId, e.ToolName,
                e.IsWrite, e.ArgsSummary, e.Decision, e.DecisionReason, e.Confirmation,
                e.Outcome, e.Error, e.DurationMs))
            .ToListAsync(ct);

        return Ok(new AuditPageDTO(total, items));
    }
}
