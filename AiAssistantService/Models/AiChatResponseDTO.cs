namespace AiAssistantService.Models;

/// <summary>
/// Response for POST /api/AiChat.
/// <see cref="Status"/> = "completed" (agent finished, see <see cref="AssistantText"/>) or
/// "pending_confirmation" (agent wants to run a write — see <see cref="Proposal"/> — and is waiting
/// for the caller to approve it on a follow-up call). <see cref="History"/> is the full conversation
/// so far; the client echoes it back on the next call.
/// </summary>
public class AiChatResponseDTO
{
    public string Status { get; set; } = "completed";

    /// <summary>Assistant's final text when <see cref="Status"/> == "completed".</summary>
    public string? AssistantText { get; set; }

    /// <summary>The proposed write when <see cref="Status"/> == "pending_confirmation".</summary>
    public ToolProposalDTO? Proposal { get; set; }

    /// <summary>Full conversation so far (client echoes this back on the next call).</summary>
    public List<ChatMessageDTO> History { get; set; } = new();

    /// <summary>Correlation id shared by every gateway tool call in this logical interaction.</summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>How many tool-call rounds were executed (bounded by the iteration cap).</summary>
    public int Iterations { get; set; }
}

/// <summary>Status constants for <see cref="AiChatResponseDTO.Status"/>.</summary>
public static class AiChatStatus
{
    public const string Completed = "completed";
    public const string PendingConfirmation = "pending_confirmation";
}
