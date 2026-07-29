using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using AiAssistantService.Agent;
using AiAssistantService.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AiAssistantServiceTests;

public class AgentTokenServiceTests
{
    private static (string privatePem, string publicPem) NewKeyPair()
    {
        using var rsa = RSA.Create(2048);
        return (rsa.ExportPkcs8PrivateKeyPem(), rsa.ExportSubjectPublicKeyInfoPem());
    }

    private static AgentTokenService CreateService(string privatePem) =>
        new(Options.Create(new AgentOptions
        {
            AgentId = "ai-assistant",
            Audience = "mcp-gateway",
            TokenLifetimeSeconds = 120,
            PrivateKeyPem = privatePem,
        }));

    // REGRESSION (found live, 2026-07-29): the AI chat worked exactly ONCE per service lifetime,
    // then every later request failed with
    //   ObjectDisposedException: 'System.Security.Cryptography.RSAOpenSsl'
    //     at AgentTokenService.CreateAgentToken()
    // surfacing to the user only as HTTP 400 + "Asistent trenutno ne moze da obradi zahtev."
    //
    // Cause: Microsoft.IdentityModel.Tokens caches SignatureProvider instances globally, keyed on
    // key material. Mint #1 cached a provider holding that call's RSA, which `using var rsa` then
    // disposed. Mint #2 imports the SAME PEM, so the cache HITS and returns the provider bound to
    // the already-disposed RSA. The old code comment claimed disposal at method exit was safe
    // "because WriteToken signs synchronously" — true of the signing call, but the library keeps
    // the provider alive past the method.
    //
    // AgentTokenService is a SINGLETON, so in production every request after the first was broken.
    // The suite missed it because each existing test mints once with a freshly generated key —
    // distinct key material means the cache never collides. This test reuses ONE key and mints
    // TWICE, which is what production actually does.
    [Fact]
    public void CreateAgentToken_can_mint_repeatedly_with_the_same_key()
    {
        var (privatePem, publicPem) = NewKeyPair();
        var service = CreateService(privatePem);

        var first = service.CreateAgentToken();
        var second = service.CreateAgentToken();
        var third = service.CreateAgentToken();

        using var pub = RSA.Create();
        pub.ImportFromPem(publicPem);
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = "ai-assistant",
            ValidAudience = "mcp-gateway",
            IssuerSigningKey = new RsaSecurityKey(pub),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };

        var handler = new JwtSecurityTokenHandler();
        foreach (var token in new[] { first, second, third })
        {
            Assert.False(string.IsNullOrWhiteSpace(token));
            // Must not throw — an ObjectDisposedException here is the regression.
            handler.ValidateToken(token, validationParameters, out _);
        }
    }

    [Fact]
    public void CreateAgentToken_mints_RS256_token_validatable_by_the_matching_public_key()
    {
        var (privatePem, publicPem) = NewKeyPair();
        var service = CreateService(privatePem);

        var token = service.CreateAgentToken();

        // Validate with the PUBLIC key of the pair — proves the gateway (which holds only the
        // public key) can verify a token the agent signed with its private key.
        using var publicRsa = RSA.Create();
        publicRsa.ImportFromPem(publicPem);

        var handler = new JwtSecurityTokenHandler();
        handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(publicRsa),
            ValidateIssuer = true,
            ValidIssuer = "ai-assistant",
            ValidateAudience = true,
            ValidAudience = "mcp-gateway",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        }, out var validated);

        var jwt = Assert.IsType<JwtSecurityToken>(validated);
        Assert.Equal(SecurityAlgorithms.RsaSha256, jwt.Header.Alg); // RS256
        Assert.Equal("ai-assistant", jwt.Issuer);
        Assert.Contains("mcp-gateway", jwt.Audiences);
    }

    [Fact]
    public void CreateAgentToken_is_rejected_by_a_different_public_key()
    {
        var (privatePem, _) = NewKeyPair();
        var (_, otherPublicPem) = NewKeyPair(); // unrelated key
        var token = CreateService(privatePem).CreateAgentToken();

        using var otherRsa = RSA.Create();
        otherRsa.ImportFromPem(otherPublicPem);

        var handler = new JwtSecurityTokenHandler();
        Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(otherRsa),
            ValidateIssuer = true,
            ValidIssuer = "ai-assistant",
            ValidateAudience = true,
            ValidAudience = "mcp-gateway",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        }, out _));
    }
}
