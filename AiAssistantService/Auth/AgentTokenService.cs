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

        // Normalize escaped newlines. A PEM supplied via a Docker/.env environment variable arrives
        // as a single physical line with literal "\n" (backslash-n) escapes, which ImportFromPem
        // cannot parse. A PEM from JSON appsettings or user-secrets already has real newlines, so this
        // is a no-op there (a base64 body never contains a literal backslash). This lets the same
        // secret be injected either way.
        var pem = _options.PrivateKeyPem.Replace("\\r\\n", "\n").Replace("\\n", "\n");

        // Own the RSA for the lifetime of the mint; WriteToken signs synchronously, then the RSA is
        // disposed at method exit. CacheSignatureProviders MUST be disabled here: a RsaSecurityKey
        // built from a raw RSA instance has an empty InternalId, so the process-wide
        // CryptoProviderFactory cache would keep this call's signature provider (holding the RSA we
        // are about to dispose) and hand it back on the NEXT mint — throwing ObjectDisposedException
        // on every subsequent call. Disabling the cache keeps the provider's lifetime inside this
        // method, matching the RSA's.
        using var rsa = RSA.Create();
        rsa.ImportFromPem(pem);

        var credentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };

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
