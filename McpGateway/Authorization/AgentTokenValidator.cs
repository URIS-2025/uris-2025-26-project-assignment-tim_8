using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace McpGateway.Authorization;

/// <summary>
/// Validates the agent-JWT presented by an MCP client (the "agent" principal). The token is
/// signed with the agent's <b>private</b> key (RS256); the gateway verifies it with the agent's
/// configured <b>public</b> key, and checks issuer / audience / lifetime. On success it resolves
/// the <see cref="AgentContext"/> (agent id + allowed tools) used by <see cref="ToolAuthorizer"/>.
/// </summary>
public class AgentTokenValidator
{
    public const string ExpectedAudience = "mcp-gateway";

    private readonly IReadOnlyDictionary<string, (SecurityKey Key, IReadOnlySet<string> AllowedTools)> _agents;
    private readonly JwtSecurityTokenHandler _handler = new();

    public AgentTokenValidator(IEnumerable<AgentPolicy> agents)
    {
        var map = new Dictionary<string, (SecurityKey, IReadOnlySet<string>)>();
        foreach (var a in agents)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(a.PublicKeyPem);
            map[a.AgentId] = (new RsaSecurityKey(rsa), a.AllowedTools.ToHashSet());
        }
        _agents = map;
    }

    /// <summary>Validates a raw agent-JWT string (from the <c>X-Agent-Token</c> header).</summary>
    public AgentAuthResult Validate(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return AgentAuthResult.Fail("nedostaje agent token");

        // Read the issuer (= agent id) WITHOUT validating, so we can pick the right public key.
        string issuer;
        try
        {
            issuer = _handler.ReadJwtToken(token).Issuer;
        }
        catch
        {
            return AgentAuthResult.Fail("nevažeći agent token");
        }

        if (!_agents.TryGetValue(issuer, out var agent))
            return AgentAuthResult.Fail($"nepoznat agent izdavalac '{issuer}'");

        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = agent.Key,
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = ExpectedAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        try
        {
            _handler.ValidateToken(token, parameters, out _);
        }
        catch (SecurityTokenException)
        {
            return AgentAuthResult.Fail("nevažeći agent token");
        }
        catch (Exception)
        {
            // Defense-in-depth: ANY validation fault fails closed (never a success). We also do
            // not leak library-internal exception details to the (untrusted) caller.
            return AgentAuthResult.Fail("neuspela validacija agent tokena");
        }

        return AgentAuthResult.Success(new AgentContext(issuer, agent.AllowedTools));
    }
}
