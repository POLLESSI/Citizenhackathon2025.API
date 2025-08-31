using CitizenHackathon2025.Domain.Enums;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CitizenHackathon2025.API.Tools 
{
    public class TokenGenerator
    {
    #nullable disable
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly int _tokenDuration;
        private readonly string _issuer;
        private readonly string? _audience;

        public TokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
            _secretKey = _configuration["Jwt:Secret"];
            _tokenDuration = int.TryParse(_configuration["JwtSettings:TokenDurationMinutes"], out int minutes) ? minutes : 30;
            _issuer = _configuration["Jwt:Issuer"] ?? "CitizenHackathon2025API";
            _audience = _configuration["Jwt:Audience"]; 
        }

        public string GetSecretKey() => _secretKey;

        public string GenerateToken(string email, UserRole role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512);

            // ✅ role aligned with your constants (case included)
            var roleValue = role switch
            {
                UserRole.Admin => Roles.Admin,
                UserRole.Modo => Roles.Modo,
                UserRole.User => Roles.User,
                UserRole.Guest => Roles.Guest,
                _ => Roles.User
            };

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                // ✅ duplicate role (historical policies compatibility + RequireRole)
                new Claim(ClaimTypes.Role, roleValue),
                new Claim(Claims.Role,     roleValue)
            };

            var jwt = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,           // can remain null if ValidateAudience=false
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(_tokenDuration),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}






































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.