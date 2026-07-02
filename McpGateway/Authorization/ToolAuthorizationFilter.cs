using System.Security.Claims;
using ModelContextProtocol.Protocol;

namespace McpGateway.Authorization;

/// <summary>
/// Adapter between the MCP call-tool filter and <see cref="RequestGatekeeper"/>. Given the inputs
/// extracted from the request (agent token, user principal, tool name), it runs the gateway's
/// authorize+audit and either short-circuits with an error result (deny) or forwards to the real
/// tool (allow). Deliberately free of MCP request-plumbing so <see cref="EvaluateAsync"/> is
/// directly unit-testable.
/// </summary>
public class ToolAuthorizationFilter
{
    private readonly RequestGatekeeper _gatekeeper;

    public ToolAuthorizationFilter(RequestGatekeeper gatekeeper)
    {
        _gatekeeper = gatekeeper;
    }

    public async ValueTask<CallToolResult> EvaluateAsync(
        string? agentToken, ClaimsPrincipal? user, string toolName, string? argsSummary,
        Func<CancellationToken, ValueTask<CallToolResult>> next, CancellationToken cancellationToken,
        IInvocationContextAccessor? invocationContext = null, string? bearerToken = null)
    {
        var decision = await _gatekeeper.AuthorizeAndAuditAsync(
            agentToken, user, toolName, argsSummary, cancellationToken: cancellationToken);

        if (!decision.IsAllowed)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = $"Zabranjeno: {decision.Reason}" }],
            };
        }

        // On allow, publish the authorized execution context for the tool body (org-scope for reads;
        // OBO bearer for writes). Only ever set on allow — a denied call never reaches a tool.
        if (invocationContext is not null)
        {
            var u = RequestGatekeeper.ExtractUser(user);
            invocationContext.Current = new InvocationContext(
                decision.EffectiveOrganizationId, u?.Role ?? string.Empty, u?.UserId ?? string.Empty,
                bearerToken);
        }

        return await next(cancellationToken);
    }
}
