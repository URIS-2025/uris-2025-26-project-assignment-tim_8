using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic;
using Anthropic.Models.Messages;
using AiAssistantService.Agent;
using AiAssistantService.Models;
using Microsoft.Extensions.Options;

namespace AiAssistantService.Clients;

/// <summary>
/// Real <see cref="IClaudeAgentClient"/> over the official Anthropic SDK. Maps the domain
/// conversation + tool set to a Messages request and maps the response back to a
/// <see cref="ClaudeResult"/>, keeping all SDK types confined to this class. Parallel tool use is
/// disabled so a turn carries at most one tool call (clean propose-confirm + audit).
/// </summary>
public class ClaudeClient : IClaudeAgentClient
{
    private readonly AnthropicClient _client;
    private readonly AgentOptions _options;

    public ClaudeClient(AnthropicClient client, IOptions<AgentOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<ClaudeResult> SendAsync(
        IReadOnlyList<ChatMessageDTO> messages,
        IReadOnlyList<ToolDefinition> tools,
        CancellationToken cancellationToken)
    {
        var parameters = BuildParams(messages, tools, _options);

        var response = await _client.Messages.Create(parameters, cancellationToken: cancellationToken);

        string? text = null;
        var toolCalls = new List<ClaudeToolCall>();
        foreach (var block in response.Content)
        {
            if (block.TryPickText(out TextBlock? textBlock))
                text = (text ?? string.Empty) + textBlock!.Text;
            else if (block.TryPickToolUse(out ToolUseBlock? toolUse))
                toolCalls.Add(new ClaudeToolCall(
                    toolUse!.ID, toolUse.Name,
                    JsonSerializer.SerializeToNode(toolUse.Input) ?? new JsonObject()));
        }

        return new ClaudeResult(text, toolCalls);
    }

    // ── domain → SDK ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the Messages request. Extracted (and <c>internal</c> rather than private) so the
    /// request shape — above all the explicit <c>Thinking</c> setting — is covered by
    /// <c>ClaudeClientParamsTests</c>; SDK types stay confined to this class.
    /// </summary>
    /// <remarks>
    /// <c>Thinking</c> is set to DISABLED deliberately, and must stay set. On
    /// <c>claude-sonnet-5</c> an OMITTED <c>thinking</c> parameter runs ADAPTIVE thinking (a silent
    /// default change from Sonnet 4.6, where omitting it meant off). With thinking on, Claude's
    /// assistant turn leads with a thinking block whose signature the API validates when the turn is
    /// echoed back — and <see cref="ChatContentDTO"/> has no "thinking" type, so this loop cannot
    /// round-trip it. Leaving it unset therefore produced an intermittent 400 on the turn right
    /// after any tool call. To enable thinking later, first teach <see cref="ChatContentDTO"/> and
    /// <c>AiChatAgent.AssistantTurn</c> to carry thinking blocks (Signature included).
    /// </remarks>
    internal static MessageCreateParams BuildParams(
        IReadOnlyList<ChatMessageDTO> messages,
        IReadOnlyList<ToolDefinition> tools,
        AgentOptions options) => new()
        {
            Model = options.Model,
            MaxTokens = options.MaxTokens,
            Thinking = new ThinkingConfigDisabled(),
            ToolChoice = new ToolChoiceAuto { DisableParallelToolUse = true },
            Tools = tools.Select(ToSdkTool).ToList(),
            Messages = messages.Select(ToSdkMessage).ToList(),
            System = string.IsNullOrWhiteSpace(options.SystemPrompt)
                ? (MessageCreateParamsSystem?)null
                : options.SystemPrompt,
        };

    private static ToolUnion ToSdkTool(ToolDefinition tool)
    {
        var properties = new Dictionary<string, JsonElement>();
        if (tool.InputSchema.TryGetProperty("properties", out var props) && props.ValueKind == JsonValueKind.Object)
            foreach (var prop in props.EnumerateObject())
                properties[prop.Name] = prop.Value;

        var required = new List<string>();
        if (tool.InputSchema.TryGetProperty("required", out var req) && req.ValueKind == JsonValueKind.Array)
            foreach (var item in req.EnumerateArray())
                if (item.GetString() is { } name)
                    required.Add(name);

        return new Tool
        {
            Name = tool.Name,
            Description = tool.Description,
            InputSchema = new() { Properties = properties, Required = required },
        };
    }

    private static MessageParam ToSdkMessage(ChatMessageDTO message)
    {
        var content = new List<ContentBlockParam>();
        foreach (var block in message.Content)
        {
            switch (block.Type)
            {
                case "text":
                    content.Add(new TextBlockParam { Text = block.Text ?? string.Empty });
                    break;
                case "tool_use":
                    content.Add(new ToolUseBlockParam
                    {
                        ID = block.ToolUseId ?? string.Empty,
                        Name = block.ToolName ?? string.Empty,
                        Input = ToInputDict(block.Input),
                    });
                    break;
                case "tool_result":
                    content.Add(new ToolResultBlockParam
                    {
                        ToolUseID = block.ToolUseId ?? string.Empty,
                        Content = block.ResultContent ?? string.Empty,
                        IsError = block.IsError,
                    });
                    break;
            }
        }

        return new MessageParam
        {
            Role = message.Role == "assistant" ? Role.Assistant : Role.User,
            Content = content,
        };
    }

    private static Dictionary<string, JsonElement> ToInputDict(JsonNode? node)
    {
        var dict = new Dictionary<string, JsonElement>();
        if (node is JsonObject obj)
            foreach (var kvp in obj)
                dict[kvp.Key] = JsonSerializer.SerializeToElement(kvp.Value);
        return dict;
    }
}
