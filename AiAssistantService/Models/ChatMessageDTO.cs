namespace AiAssistantService.Models;

/// <summary>One turn in the conversation: a role ("user" | "assistant") and its content blocks.</summary>
public class ChatMessageDTO
{
    public string Role { get; set; } = "user";
    public List<ChatContentDTO> Content { get; set; } = new();
}
