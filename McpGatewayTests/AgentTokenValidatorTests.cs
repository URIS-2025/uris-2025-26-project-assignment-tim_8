using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using McpGateway.Authorization;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace McpGatewayTests;

public class AgentTokenValidatorTests
{
    private const string AgentId = "ai-assistant";
    private static readonly string[] AllowedTools = ["get_org_overview", "get_problem_stats"];

    // Mint an agent-JWT signed with the given PRIVATE key.
    private static string MintToken(RSA privateKey, string issuer, string audience, DateTime expires)
    {
        var creds = new SigningCredentials(new RsaSecurityKey(privateKey), SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(issuer: issuer, audience: audience, claims: null,
            notBefore: null, expires: expires, signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // A validator whose registry trusts only the PUBLIC half of the given key for AgentId.
    private static AgentTokenValidator BuildValidator(RSA keyPair) =>
        new([new AgentPolicy(AgentId, keyPair.ExportSubjectPublicKeyInfoPem(), AllowedTools)]);

    [Fact]
    public void Accepts_a_valid_agent_token_and_resolves_allowed_tools()
    {
        using var rsa = RSA.Create(2048);
        var validator = BuildValidator(rsa);
        var token = MintToken(rsa, AgentId, AgentTokenValidator.ExpectedAudience, DateTime.UtcNow.AddMinutes(5));

        var result = validator.Validate(token);

        Assert.True(result.Succeeded);
        Assert.Equal(AgentId, result.Agent!.AgentId);
        Assert.Contains("get_problem_stats", result.Agent.AllowedTools);
    }

    [Fact] // T1 — missing / empty token
    public void Rejects_missing_token()
    {
        using var rsa = RSA.Create(2048);
        var validator = BuildValidator(rsa);

        Assert.False(validator.Validate(null).Succeeded);
    }

    [Fact] // T1 — expired token
    public void Rejects_expired_token()
    {
        using var rsa = RSA.Create(2048);
        var validator = BuildValidator(rsa);
        var token = MintToken(rsa, AgentId, AgentTokenValidator.ExpectedAudience, DateTime.UtcNow.AddMinutes(-10));

        Assert.False(validator.Validate(token).Succeeded);
    }

    [Fact] // T1 — issuer (agent) not in the registry
    public void Rejects_unknown_issuer()
    {
        using var rsa = RSA.Create(2048);
        var validator = BuildValidator(rsa);
        var token = MintToken(rsa, "ghost-agent", AgentTokenValidator.ExpectedAudience, DateTime.UtcNow.AddMinutes(5));

        var result = validator.Validate(token);

        Assert.False(result.Succeeded);
        Assert.Contains("izdavalac", result.Error!);
    }

    [Fact] // T1 — audience is not this gateway
    public void Rejects_wrong_audience()
    {
        using var rsa = RSA.Create(2048);
        var validator = BuildValidator(rsa);
        var token = MintToken(rsa, AgentId, "some-other-service", DateTime.UtcNow.AddMinutes(5));

        Assert.False(validator.Validate(token).Succeeded);
    }

    [Fact] // T1 — CRUCIAL: token signed by a key the gateway does NOT trust (forgery attempt)
    public void Rejects_token_signed_by_untrusted_key()
    {
        using var trusted = RSA.Create(2048);
        using var attacker = RSA.Create(2048);
        var validator = BuildValidator(trusted);   // gateway trusts only 'trusted' public key for AgentId
        var token = MintToken(attacker, AgentId, AgentTokenValidator.ExpectedAudience, DateTime.UtcNow.AddMinutes(5));

        Assert.False(validator.Validate(token).Succeeded);   // signed by 'attacker' → rejected
    }

    [Fact] // malformed input (not a JWT)
    public void Rejects_malformed_token()
    {
        using var rsa = RSA.Create(2048);
        var validator = BuildValidator(rsa);

        Assert.False(validator.Validate("this-is-not-a-jwt").Succeeded);
    }
}
