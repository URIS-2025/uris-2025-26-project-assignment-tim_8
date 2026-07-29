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

        // Own the RSA for the lifetime of the mint. Disposing at method exit is only safe because
        // signature-provider caching is turned OFF below — see the comment on CryptoProviderFactory.
        using var rsa = RSA.Create();
        rsa.ImportFromPem(pem);

        // CacheSignatureProviders = false is LOAD-BEARING, not a micro-optimisation.
        //
        // Microsoft.IdentityModel.Tokens caches SignatureProvider instances globally, keyed on key
        // material. With caching on (the default), mint #1 cached a provider holding THIS call's
        // RSA; `using` then disposed that RSA at method exit. Mint #2 imports the same PEM, so the
        // cache HIT returned the provider still bound to the disposed RSA and threw
        //   ObjectDisposedException: 'System.Security.Cryptography.RSAOpenSsl'
        // from WriteToken. Because this service is a SINGLETON and the key never changes, that made
        // the AI chat work exactly ONCE per process lifetime — every later request became an
        // HTTP 400 behind the fixed "Asistent trenutno ne moze da obradi zahtev." message.
        //
        // Opting out gives each mint its own provider, so per-call RSA ownership is correct. The
        // alternative (hold one RSA for the process lifetime and let the cache work) would also fix
        // it, but would make this class IDisposable and tie key lifetime to the container.
        //
        // Regression test: AgentTokenServiceTests.CreateAgentToken_can_mint_repeatedly_with_the_same_key
        var credentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false },
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
