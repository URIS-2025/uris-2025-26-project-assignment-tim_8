using AiAssistantService.Models;

namespace AiAssistantService.Agent;

/// <summary>
/// The agentic loop: runs Claude against the gateway tools on behalf of the caller, executing read
/// tools automatically and pausing on write tools for human-in-the-loop confirmation. All security
/// (dual-principal, per-tool authz, audit) is enforced by the gateway; this loop only orchestrates.
/// </summary>
public interface IAiChatAgent
{
    /// <summary>
    /// Run one /api/AiChat turn. <paramref name="oboBearer"/> is the caller's raw Authorization header
    /// value ("Bearer &lt;jwt&gt;"), forwarded to the gateway as the on-behalf-of principal.
    /// </summary>
    Task<AiChatResponseDTO> RunAsync(AiChatRequestDTO request, string oboBearer, CancellationToken cancellationToken);
}
