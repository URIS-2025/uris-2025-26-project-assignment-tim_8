using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SystemNotificationService.Tests
{
    // Mints a JWT that satisfies the SystemNotificationService [Authorize] validation
    // (issuer/audience/key must be one of those listed in appsettings.json "Jwt").
    // Uses the OrganizationService dev key/issuer — the token an org user (the frontend
    // org page) would present.
    internal static class TestJwt
    {
        private const string Key = "OrganizationService-Dev-Key-Replace-In-Production-32chars!";
        private const string Issuer = "OrganizationService";
        private const string Audience = "OrganizationService";

        public static string Create()
        {
            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                claims: new[] { new Claim(ClaimTypes.Name, "integration-test-user") },
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
