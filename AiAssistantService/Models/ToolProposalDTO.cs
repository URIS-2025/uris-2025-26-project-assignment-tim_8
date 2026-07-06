namespace AiAssistantService.Models;

/// <summary>
/// A proposed WRITE tool call surfaced to the caller for human-in-the-loop confirmation.
/// The agent NEVER executes a write before the caller approves it (see the propose-confirm flow).
/// <see cref="ArgsSummary"/> is a scrubbed, display-only summary — never a raw secret (e.g. a box password).
/// </summary>
public class ToolProposalDTO
{
    public string ToolUseId { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public string ArgsSummary { get; set; } = string.Empty;
}
