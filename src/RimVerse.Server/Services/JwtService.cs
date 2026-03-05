using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace RimVerse.Server.Services
{
    public class JwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(Guid playerId, string displayName, string role)
        {
            var secret = _config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expirationHours = int.Parse(_config["Jwt:ExpirationHours"] ?? "24");

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, playerId.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, displayName),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "rimverse-server",
                audience: _config["Jwt:Audience"] ?? "rimverse-client",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expirationHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public DateTime GetExpiration()
        {
            var expirationHours = int.Parse(_config["Jwt:ExpirationHours"] ?? "24");
            return DateTime.UtcNow.AddHours(expirationHours);
        }
    }
}
