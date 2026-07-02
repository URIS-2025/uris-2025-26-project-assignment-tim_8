using McpGateway.Authorization;

namespace McpGateway.Tools;

/// <summary>
/// Resolves the organization a read tool must operate on, from the authorized
/// <see cref="InvocationContext"/> plus any org the caller requested as a tool argument. This is the
/// read-side org-scope guard: a manager is ALWAYS locked to their own org (a requested org is
/// ignored — the LLM can never widen scope), while an admin may optionally target a specific org
/// (null = all orgs).
/// </summary>
public static class ToolScope
{
    /// <returns>
    /// The effective organization id: non-null = scope to exactly this org; null = no org scope
    /// (admin/global). For a manager this is always their own org, regardless of
    /// <paramref name="requestedOrganizationId"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">The invocation context is missing (authorization
    /// did not run) — fail closed rather than run an unscoped query.</exception>
    public static Guid? Resolve(InvocationContext? context, Guid? requestedOrganizationId)
    {
        if (context is null)
            throw new InvalidOperationException(
                "nedostaje kontekst poziva — autorizacija nije postavila InvocationContext");

        // Manager (or any org-locked caller): forced to own org; requested org is IGNORED.
        if (context.EffectiveOrganizationId is Guid forced)
            return forced;

        // Admin / non-scoped: may optionally target one org; null means all orgs.
        return requestedOrganizationId;
    }
}
