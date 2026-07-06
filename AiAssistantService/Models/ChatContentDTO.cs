using System.Text.Json.Nodes;

namespace AiAssistantService.Models;

/// <summary>
/// One content block inside a chat turn. Flat (Type-discriminated) so it round-trips through
/// System.Text.Json without polymorphic-serializer configuration. This is BOTH the wire shape
/// (the client echoes the whole conversation back so the server stays stateless — "lightweight
/// history") AND the conversation representation the agent loop consumes.
/// </summary>
/// <remarks>
/// <see cref="Type"/> ∈ { "text", "tool_use", "tool_result" }:
///  - text        → <see cref="Text"/>
///  - tool_use    → <see cref="ToolUseId"/>, <see cref="ToolName"/>, <see cref="Input"/>
///  - tool_result → <see cref="ToolUseId"/>, <see cref="ResultContent"/>, <see cref="IsError"/>
/// </remarks>
public class ChatContentDTO
{
    public string Type { get; set; } = "text";
    public string? Text { get; set; }
    public string? ToolUseId { get; set; }
    public string? ToolName { get; set; }
    public JsonNode? Input { get; set; }
    public string? ResultContent { get; set; }
    public bool IsError { get; set; }
}
