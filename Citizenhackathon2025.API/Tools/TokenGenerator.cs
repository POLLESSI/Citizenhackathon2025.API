using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Citizenhackathon2025.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace CityzenHackathon2025.API.Tools
{
    public class TokenGenerator
    {
#nullable disable
        public static string secretKey = "µpiçaezjrkuyjfgk:ghmkjghmiugl:hjfvtFSDMOifnZAE MOVjkµ$)'éàipornjfd ù)'$piçhbc";
        public string GenerateToken(string email, string role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512);
            Claim[] userInfo = new[]
            {
                new Claim(ClaimTypes.Role, role == "admin" ? "admin" : role == "modo" ? "modo" : "user"),
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(ClaimTypes.Sid, role.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };
            JwtSecurityToken jwt = new JwtSecurityToken(
                //issuer: "yourdomain.com",
                //audience: "yourdomain.com",
                claims: userInfo,
                signingCredentials: credentials,
                expires: DateTime.Now.AddMinutes(30)
            );
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(jwt);
        }
    }
}
