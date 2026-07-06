using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using AiAssistantService.Agent;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AiAssistantService.Auth;

/// <inheritdoc />
public class AgentTokenService : IAgentTokenService
{
    private readonly AgentOptions _options;

    public AgentTokenService(IOptions<AgentOptions> options)
    {
        _options = options.Value;
    }

    public string CreateAgentToken()
    {
        if (string.IsNullOrWhiteSpace(_options.PrivateKeyPem))
            throw new InvalidOperationException("Agent privatni kljuc (Agent:PrivateKeyPem) nije konfigurisan.");

        // Own the RSA for the lifetime of the mint; WriteToken signs synchronously, so disposing at
        // method exit (after the token string is produced) is safe.
        using var rsa = RSA.Create();
        rsa.ImportFromPem(_options.PrivateKeyPem);

        var credentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _options.AgentId,
            audience: _options.Audience,
            claims: null,
            notBefore: now,
            expires: now.AddSeconds(_options.TokenLifetimeSeconds),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
