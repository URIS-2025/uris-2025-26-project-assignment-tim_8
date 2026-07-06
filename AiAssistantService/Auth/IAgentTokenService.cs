namespace AiAssistantService.Auth;

/// <summary>
/// Mints the agent-JWT (the agent principal) that the gateway validates with the agent's PUBLIC key.
/// Signed with the agent's PRIVATE RSA key (RS256), issuer "ai-assistant", audience "mcp-gateway",
/// short lifetime. The private key is a secret (env / user-secrets), never in the repo.
/// </summary>
public interface IAgentTokenService
{
    /// <summary>Create a short-lived, signed agent-JWT for one gateway interaction.</summary>
    string CreateAgentToken();
}
