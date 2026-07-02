namespace McpGateway.Authorization;

/// <summary>
/// The authorized execution context for a single (allowed) tool call, computed by the gateway's
/// authorization filter and made available to the tool body. Read tools use
/// <see cref="EffectiveOrganizationId"/> to scope their queries; write tools (later phase) use
/// <see cref="BearerToken"/> to forward the caller's on-behalf-of JWT to the REST endpoints.
/// </summary>
/// <param name="EffectiveOrganizationId">
/// The organization the call is locked to. Non-null = forced org (a manager may only ever see/act
/// on this org). Null = no org lock (an admin, who may optionally target any org, or a global tool).
/// The LLM can never widen scope: this value comes from the validated token, not from tool args.
/// </param>
/// <param name="UserRole">The caller's role (e.g. "Admin"/"Manager"), original-case from the OBO JWT.</param>
/// <param name="UserId">The caller's user id from the OBO JWT.</param>
/// <param name="BearerToken">The raw OBO bearer header value, for forwarding to REST (write tools, later).</param>
public record InvocationContext(
    Guid? EffectiveOrganizationId,
    string UserRole,
    string UserId,
    string? BearerToken);

/// <summary>
/// Per-request holder for the current <see cref="InvocationContext"/>. Registered <b>scoped</b> so
/// the authorization filter (which runs in the request's DI scope) and the tool body (resolved from
/// the same scope) share one instance. Deliberately mirrors the <c>IHttpContextAccessor</c> shape.
/// </summary>
public interface IInvocationContextAccessor
{
    InvocationContext? Current { get; set; }
}

/// <inheritdoc />
public sealed class InvocationContextAccessor : IInvocationContextAccessor
{
    public InvocationContext? Current { get; set; }
}
