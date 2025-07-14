using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CitizenHackathon2025.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CityzenHackathon2025.API.Tools
{
    public class TokenGenerator
    {
    #nullable disable
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly int _tokenDuration;

        public TokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
            _secretKey = _configuration["JwtSettings:SecretKey"];
            _tokenDuration = int.TryParse(_configuration["JwtSettings:TokenDurationMinutes"], out int minutes)
                ? minutes
                : 30; // fallback if the value is not readable
        }
        public string GetSecretKey()
        {
            return _secretKey;
        }
        public string GenerateToken(string email, UserRole role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512);

            string roleValue = role.ToRoleString(); // "admin", "modo", "user"

            var claims = new[]
            {
                new Claim(ClaimTypes.Role, roleValue),
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, email)
            };

            var jwt = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_tokenDuration),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}






































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.