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
