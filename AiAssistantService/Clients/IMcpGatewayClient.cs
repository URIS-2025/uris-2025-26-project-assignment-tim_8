using System.Text.Json.Nodes;
using AiAssistantService.Agent;

namespace AiAssistantService.Clients;

/// <summary>
/// MCP client to the McpGateway (<c>/mcp</c>). Every call carries BOTH principals — the caller's
/// OBO bearer and the agent's minted agent-JWT — plus a correlation id, via transport headers
/// (Authorization / X-Agent-Token / X-Correlation-Id). A fresh transport is created per call
/// because the OBO token is per-request and the gateway is stateless.
/// </summary>
public interface IMcpGatewayClient
{
    /// <summary>List the tools the gateway exposes (subject to per-tool authorization on call).</summary>
    Task<IReadOnlyList<ToolDefinition>> ListToolsAsync(
        string oboBearer, string agentToken, string correlationId, CancellationToken cancellationToken);

    /// <summary>Invoke a tool through the gateway. Tool-level failures come back as
    /// <see cref="ToolCallResult.IsError"/>=true, not exceptions.</summary>
    Task<ToolCallResult> CallToolAsync(
        string toolName, JsonNode input,
        string oboBearer, string agentToken, string correlationId, CancellationToken cancellationToken);
}
