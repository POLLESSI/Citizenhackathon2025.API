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
        public string GenerateTokenFromPrincipal(ClaimsPrincipal principal, int expiresInMinutes = 5)
        {
            // Sécurité : clé & algo
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            // Email/Name (fallbacks robustes)
            var email =
                   principal.FindFirstValue(ClaimTypes.Email)
                ?? principal.FindFirstValue(JwtRegisteredClaimNames.Email)
                ?? principal.FindFirstValue(ClaimTypes.Name)
                ?? principal.Identity?.Name
                ?? "unknown@local";

            // Rôles : on récupère tous les rôles présents
            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToList();
            if (roles.Count == 0)
            {
                // fallback : cherche un claim "role" custom si besoin
                var role = principal.FindFirst("role")?.Value
                           ?? CitizenHackathon2025.Shared.StaticConfig.Constants.Roles.User;
                roles.Add(role);
            }

            // Base claims (nouveau JTI, scope typés pour le hub)
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub,   email),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Email,              email),
                new Claim(ClaimTypes.Name,               email),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new Claim("typ",   "hub"),
                new Claim("scope", "signalr")
            };

            // Ajoute les rôles (en double format si tu utilises un Claims.Role custom)
            foreach (var r in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, r));
                claims.Add(new Claim(CitizenHackathon2025.Shared.StaticConfig.Constants.Claims.Role, r));
            }

            // (Optionnel) Tu peux recopier d'autres claims utiles du principal ici
            // ex: NameIdentifier, custom tenant/eventId, etc.
            var nameId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(nameId))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, nameId));

            // TTL court (par défaut 5 min)
            var now = DateTime.UtcNow;
            var exp = now.AddMinutes(Math.Max(1, expiresInMinutes));

            var jwt = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience, // peut être null si ValidateAudience=false
                claims: claims,
                notBefore: now,
                expires: exp,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}






































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.