using System.Diagnostics;
using System.Security.Claims;
using McpGateway.Audit;
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
        IInvocationContextAccessor? invocationContext = null, string? bearerToken = null,
        Guid? correlationId = null)
    {
        var decision = await _gatekeeper.AuthorizeAndAuditAsync(
            agentToken, user, toolName, argsSummary, correlationId: correlationId,
            cancellationToken: cancellationToken);

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

        // Execute the tool, timing it, then enrich the audit record with the outcome (best-effort:
        // the gatekeeper swallows any enrichment failure — the action already happened).
        var start = Stopwatch.GetTimestamp();
        CallToolResult result;
        try
        {
            result = await next(cancellationToken);
        }
        catch
        {
            var failedMs = (int)Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            await _gatekeeper.EnrichOutcomeAsync(
                decision.AuditId, AuditOutcome.Error, failedMs, "interna greška pri izvršenju alata", cancellationToken);
            throw;
        }

        var durationMs = (int)Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        var isError = result.IsError == true;
        await _gatekeeper.EnrichOutcomeAsync(
            decision.AuditId,
            isError ? AuditOutcome.Error : AuditOutcome.Success,
            durationMs,
            isError ? SanitizedText(result) : null,
            cancellationToken);

        return result;
    }

    /// <summary>The tool's own (already-sanitized) message text, concatenated and length-bounded —
    /// never a raw server body (write tools sanitize before returning). Safe to store in the audit.</summary>
    private static string? SanitizedText(CallToolResult result)
    {
        var text = string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text));
        if (string.IsNullOrEmpty(text)) return null;
        return text.Length > 500 ? text[..500] : text;
    }
}
