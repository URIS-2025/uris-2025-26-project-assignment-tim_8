using System.Text.Json.Nodes;
using AiAssistantService.Agent;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace AiAssistantService.Clients;

/// <inheritdoc />
public class McpGatewayClient : IMcpGatewayClient
{
    private readonly string _mcpUrl;

    public McpGatewayClient(string mcpUrl)
    {
        _mcpUrl = mcpUrl;
    }

    public async Task<IReadOnlyList<ToolDefinition>> ListToolsAsync(
        string oboBearer, string agentToken, string correlationId, CancellationToken cancellationToken)
    {
        await using var client = await CreateClientAsync(oboBearer, agentToken, correlationId, cancellationToken);
        var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
        return tools.Select(t => new ToolDefinition(t.Name, t.Description ?? string.Empty, t.JsonSchema)).ToList();
    }

    public async Task<ToolCallResult> CallToolAsync(
        string toolName, JsonNode input,
        string oboBearer, string agentToken, string correlationId, CancellationToken cancellationToken)
    {
        await using var client = await CreateClientAsync(oboBearer, agentToken, correlationId, cancellationToken);
        var result = await client.CallToolAsync(toolName, ToArguments(input), cancellationToken: cancellationToken);

        var text = string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text));
        return new ToolCallResult(text, result.IsError ?? false);
    }

    /// <summary>Fresh per-call MCP client carrying BOTH principals + the correlation id as transport
    /// headers. The gateway is stateless and the OBO token is per-request, so a per-call client is
    /// both correct and cheap enough for a demo.</summary>
    private async Task<McpClient> CreateClientAsync(
        string oboBearer, string agentToken, string correlationId, CancellationToken cancellationToken)
    {
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(_mcpUrl),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = oboBearer,
                ["X-Agent-Token"] = agentToken,
                ["X-Correlation-Id"] = correlationId,
            },
        });
        return await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    }

    private static Dictionary<string, object?> ToArguments(JsonNode input)
    {
        var dict = new Dictionary<string, object?>();
        if (input is JsonObject obj)
            foreach (var kvp in obj)
                dict[kvp.Key] = kvp.Value;
        return dict;
    }
}
