namespace McpGateway.Authorization;

public enum AuthDecision { Allow, Deny }

/// <summary>
/// Outcome of a per-tool authorization check. <see cref="EffectiveOrganizationId"/> is the
/// organization the call is locked to (forced for org-scoped, non-admin callers so the LLM can
/// never widen scope); null means "no org scoping" (admins / global tools).
/// </summary>
public record AuthResult(AuthDecision Decision, string Reason, Guid? EffectiveOrganizationId)
{
    public bool IsAllowed => Decision == AuthDecision.Allow;

    /// <summary>
    /// The id of the audit record written for this decision (set by the gatekeeper after it audits),
    /// so the caller can enrich that same row with the execution outcome. Null if not yet audited.
    /// </summary>
    public Guid? AuditId { get; init; }

    public static AuthResult Allow(Guid? effectiveOrganizationId) =>
        new(AuthDecision.Allow, "OK", effectiveOrganizationId);

    public static AuthResult Deny(string reason) =>
        new(AuthDecision.Deny, reason, null);
}
