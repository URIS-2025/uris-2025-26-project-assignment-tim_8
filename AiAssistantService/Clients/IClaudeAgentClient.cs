using AiAssistantService.Agent;
using AiAssistantService.Models;

namespace AiAssistantService.Clients;

/// <summary>
/// Abstraction over the Anthropic SDK so the agent loop is unit-testable without real network calls.
/// The concrete <c>ClaudeClient</c> maps the domain conversation (<see cref="ChatMessageDTO"/>) and
/// tool set to Anthropic request types and back, keeping SDK types out of the loop.
/// </summary>
public interface IClaudeAgentClient
{
    /// <summary>Ask Claude for the next turn given the conversation so far and the available tools.</summary>
    Task<ClaudeResult> SendAsync(
        IReadOnlyList<ChatMessageDTO> messages,
        IReadOnlyList<ToolDefinition> tools,
        CancellationToken cancellationToken);
}
