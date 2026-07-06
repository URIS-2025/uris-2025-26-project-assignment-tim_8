using System.Text.Json;
using System.Text.Json.Nodes;

namespace AiAssistantService.Agent;

/// <summary>A tool advertised by the gateway (name + description + JSON-schema for its input).</summary>
public record ToolDefinition(string Name, string Description, JsonElement InputSchema);

/// <summary>A single tool invocation Claude asked for in a turn.</summary>
public record ClaudeToolCall(string ToolUseId, string ToolName, JsonNode Input);

/// <summary>
/// Result of one Claude turn (SDK types stay inside <c>ClaudeClient</c>). When
/// <see cref="ToolCalls"/> is non-empty, Claude stopped to call tools; otherwise it finished
/// its turn and <see cref="AssistantText"/> is the answer.
/// </summary>
public record ClaudeResult(string? AssistantText, IReadOnlyList<ClaudeToolCall> ToolCalls)
{
    public bool WantsTools => ToolCalls.Count > 0;
}

/// <summary>Outcome of executing a tool through the gateway. Never throws for a tool-level error —
/// an <see cref="IsError"/>=true result is fed back to Claude as an error tool_result.</summary>
public record ToolCallResult(string Content, bool IsError);
