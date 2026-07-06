namespace AiAssistantService.Models;

/// <summary>
/// The caller's decision on a previously proposed write. Sent back on the follow-up /api/AiChat call.
/// <see cref="ToolUseId"/> must match the <see cref="ToolProposalDTO.ToolUseId"/> that was proposed.
/// </summary>
public class PendingConfirmationDTO
{
    public string ToolUseId { get; set; } = string.Empty;
    public bool Approved { get; set; }
}
