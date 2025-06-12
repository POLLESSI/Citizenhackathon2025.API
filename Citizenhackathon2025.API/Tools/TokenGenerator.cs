using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Citizenhackathon2025.Domain.Enums;
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

        public string GenerateToken(string email, Role role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512);

            string roleValue = role.ToString().ToLower(); // "admin", "modo", "user"

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