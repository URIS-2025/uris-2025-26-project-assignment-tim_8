namespace AiAssistantService.Models;

/// <summary>
/// Request body for POST /api/AiChat. The conversation is carried client-side (stateless server):
/// the caller echoes the prior <see cref="History"/> back and, when responding to a proposed write,
/// sets <see cref="PendingConfirmation"/>. <see cref="CorrelationId"/> is echoed back so the whole
/// logical interaction (reads before the proposal + the write after confirm) shares one audit id.
/// </summary>
public class AiChatRequestDTO
{
    /// <summary>The user's new natural-language message. Optional when only confirming a pending write.</summary>
    public string? Message { get; set; }

    /// <summary>Prior conversation turns, echoed back by the client (empty/absent on the first call).</summary>
    public List<ChatMessageDTO>? History { get; set; }

    /// <summary>Set when approving/rejecting a write the agent previously proposed.</summary>
    public PendingConfirmationDTO? PendingConfirmation { get; set; }

    /// <summary>Correlation id from a prior response; a new one is generated when absent.</summary>
    public string? CorrelationId { get; set; }
}
