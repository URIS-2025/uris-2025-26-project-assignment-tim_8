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

    public static AuthResult Allow(Guid? effectiveOrganizationId) =>
        new(AuthDecision.Allow, "OK", effectiveOrganizationId);

    public static AuthResult Deny(string reason) =>
        new(AuthDecision.Deny, reason, null);
}
