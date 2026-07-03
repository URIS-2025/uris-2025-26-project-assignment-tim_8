using System.Security.Claims;
using McpGateway.Audit;
using Microsoft.Extensions.Logging;

namespace McpGateway.Authorization;

/// <summary>
/// Orchestrates a single tool call's security: validate the agent-JWT, resolve the user from the
/// OBO claims, run the per-tool authorization, and write the audit record — for BOTH allow and
/// deny. Framework-agnostic (takes a raw agent token + a <see cref="ClaimsPrincipal"/>), so the
/// whole gateway decision+audit pipeline is unit-testable without the MCP/HTTP stack. The MCP
/// call filter is a thin adapter over this.
/// </summary>
public class RequestGatekeeper
{
    private readonly AgentTokenValidator _agentValidator;
    private readonly ToolAuthorizer _authorizer;
    private readonly PolicyStore _policies;
    private readonly IAuditSink _audit;
    private readonly ILogger<RequestGatekeeper> _logger;

    public RequestGatekeeper(AgentTokenValidator agentValidator, ToolAuthorizer authorizer,
        PolicyStore policies, IAuditSink audit, ILogger<RequestGatekeeper> logger)
    {
        _agentValidator = agentValidator;
        _authorizer = authorizer;
        _policies = policies;
        _audit = audit;
        _logger = logger;
    }

    /// <summary>
    /// Authorizes <paramref name="toolName"/> for the given agent token + user, records an audit
    /// entry (allow or deny), and returns the decision.
    /// </summary>
    public async Task<AuthResult> AuthorizeAndAuditAsync(
        string? agentToken, ClaimsPrincipal? user, string toolName,
        string? argsSummary = null, Guid? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var agentId = "unknown";
        UserContext? userCtx = null;
        AuthResult result;

        // Compute the decision. Any unexpected fault here still fails closed AND gets audited.
        try
        {
            var agentResult = _agentValidator.Validate(agentToken);
            userCtx = ExtractUser(user);
            agentId = agentResult.Agent?.AgentId ?? "unknown";

            // Both principals must check out before we even consult the per-tool policy.
            if (!agentResult.Succeeded)
                result = AuthResult.Deny(agentResult.Error!);
            else if (userCtx is null)
                result = AuthResult.Deny("nedostaje ili nevažeći korisnički token");
            else
                result = _authorizer.Authorize(toolName, agentResult.Agent!, userCtx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greška u autorizaciji alata {Tool}; odbijam", toolName);
            result = AuthResult.Deny("interna greška u autorizaciji");
        }

        // Every decision — allow or deny — is durably audited. A failed audit write fails the call
        // CLOSED: no action is ever executed without a corresponding audit record. The returned id
        // lets the caller enrich this same record with the execution outcome (see EnrichOutcomeAsync).
        try
        {
            var auditId = await RecordAsync(agentId, userCtx, toolName, argsSummary, correlationId, result, cancellationToken);
            result = result with { AuditId = auditId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Revizioni zapis nije uspeo za alat {Tool}; odbijam zahtev", toolName);
            return AuthResult.Deny("revizioni zapis nije uspeo — zahtev odbijen");
        }

        return result;
    }

    /// <summary>
    /// Best-effort enrichment of an already-audited call with its execution outcome. Runs AFTER the
    /// tool executed, so a failure here MUST NOT fail the request (the action already happened) — it
    /// is logged and swallowed. A null <paramref name="auditId"/> is a no-op.
    /// </summary>
    public async Task EnrichOutcomeAsync(
        Guid? auditId, AuditOutcome outcome, int durationMs, string? error, CancellationToken ct)
    {
        if (auditId is not Guid id) return; // never audited (e.g. audit-write failed) → nothing to enrich
        try
        {
            await _audit.UpdateOutcomeAsync(id, outcome, durationMs, error, ct);
        }
        catch (Exception ex)
        {
            // Best-effort: the tool already ran; a failed enrichment must not fail the request.
            _logger.LogWarning(ex, "Obogaćivanje ishoda audita nije uspelo za {AuditId}", id);
        }
    }

    private async Task<Guid> RecordAsync(string agentId, UserContext? user, string toolName,
        string? argsSummary, Guid? correlationId, AuthResult result, CancellationToken ct)
    {
        var isWrite = _policies.ToolPolicies.TryGetValue(toolName, out var policy) && policy.IsWrite;
        var entry = new AuditEntry
        {
            CorrelationId = correlationId,
            AgentId = agentId,
            UserId = user?.UserId ?? "unknown",
            UserRole = user?.Role ?? "unknown",
            OrganizationId = user?.OrganizationId,
            ToolName = toolName,
            IsWrite = isWrite,
            ArgsSummary = argsSummary,
            Decision = result.Decision,
            DecisionReason = result.Reason,
            Confirmation = result.IsAllowed && isWrite ? ConfirmationState.Proposed : null,
            Outcome = AuditOutcome.NotExecuted, // decision-level audit; execution outcome enriched later
        };
        await _audit.RecordAsync(entry, ct);
        return entry.Id;
    }

    /// <summary>Maps the OBO JWT claims (OrganizationService token) to a <see cref="UserContext"/>.</summary>
    internal static UserContext? ExtractUser(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true) return null;

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = user.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role)) return null;

        var orgRaw = user.FindFirst("OrganizationId")?.Value;
        Guid? org = Guid.TryParse(orgRaw, out var g) ? g : null;
        return new UserContext(userId, role, org);
    }
}
