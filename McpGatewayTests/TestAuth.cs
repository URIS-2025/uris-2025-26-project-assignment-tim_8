using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using McpGateway.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace McpGatewayTests;

/// <summary>Shared test helpers: minting agent-JWTs and building user principals.</summary>
internal static class TestAuth
{
    public static string MintAgentToken(RSA privateKey, string issuer, DateTime expires,
        string audience = AgentTokenValidator.ExpectedAudience)
    {
        var creds = new SigningCredentials(new RsaSecurityKey(privateKey), SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(issuer: issuer, audience: audience, claims: null,
            notBefore: null, expires: expires, signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>An authenticated user principal shaped like the OrganizationService OBO JWT.</summary>
    public static ClaimsPrincipal Principal(string userId, string role, Guid? orgId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role),
        };
        if (orgId is not null)
            claims.Add(new Claim("OrganizationId", orgId.ToString()!));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }
}
