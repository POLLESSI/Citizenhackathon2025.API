﻿using Citizenhackathon2025.Application.Extensions;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Enums;
using Citizenhackathon2025.Infrastructure.Services;
using Citizenhackathon2025.Shared.DTOs;
using CitizenHackathon2025.Shared.Interfaces;
using CitizenHackathon2025.Shared.Utils;
using CityzenHackathon2025.API.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CitizenHackathon2025.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;
        private readonly TokenGenerator _tokenGenerator;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IPasswordHasher _passwordHasher;

        public AuthController(IUserService userService, ILogger<AuthController> logger, TokenGenerator tokenGenerator, IRefreshTokenService refreshTokenService, IPasswordHasher passwordHasher)
        {
            _userService = userService;
            _logger = logger;
            _tokenGenerator = tokenGenerator;
            _refreshTokenService = refreshTokenService;
            _passwordHasher = passwordHasher;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _userService.LoginAsync(loginDto.Email, loginDto.Password);
            if (!success)
            {
                _logger.LogWarning("❌ Invalid login attempt for email: {Email}", loginDto.Email);
                return Unauthorized("Invalid credentials.");
            }

            var user = await _userService.GetUserByEmailAsync(loginDto.Email);
            if (user == null)
            {
                _logger.LogError("🔒 User not found after successful login.");
                return Unauthorized("User not found.");
            }

            var accessToken = _tokenGenerator.GenerateToken(user.Email, user.Role);
            var refreshToken = await _refreshTokenService.GenerateAsync(user.Email);

            HttpContext.Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(1)
            });

            HttpContext.Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(1)
            });

            _logger.LogInformation("✅ Login successful, token issued for: {Email}", user.Email);

            return Ok(new
            {
                message = "Login successful",
                email = user.Email,
                role = user.Role.ToString().ToLower()
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["RefreshToken"];
            var accessToken = Request.Cookies["AccessToken"];

            if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(accessToken))
                return Unauthorized("Missing token.");

            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken token;

            try
            {
                token = handler.ReadJwtToken(accessToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("🔒 Invalid JWT token format: {Error}", ex.Message);
                return Unauthorized("Invalid token.");
            }

            var email = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var role = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            if (email == null || role == null)
                return Unauthorized("Invalid token payload.");

            if (!Enum.TryParse<Role>(role, true, out var parsedRole))
                return Unauthorized("Invalid role in token.");

            // 🔐 Validating the refreshToken
            if (!await _refreshTokenService.ValidateAsync(refreshToken))
                return Unauthorized("Invalid or expired refresh token.");

            // ♻️ Invalidation of the old + generation of a new one
            await _refreshTokenService.InvalidateAsync(refreshToken);
            var newRefreshToken = await _refreshTokenService.GenerateAsync(email);

            // 🔑 Generating the new accessToken
            var newAccessToken = _tokenGenerator.GenerateToken(email, parsedRole);

            // ⏳ Cookies update
            HttpContext.Response.Cookies.Append("AccessToken", newAccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(30)
            });

            HttpContext.Response.Cookies.Append("RefreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(1)
            });

            _logger.LogInformation("🔁 Token refreshed for: {Email}", email);

            return Ok(new { message = "Token refreshed" });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDTO userDto)
        {
            if (userDto == null || string.IsNullOrWhiteSpace(userDto.Email) || string.IsNullOrWhiteSpace(userDto.Pwd))
                return BadRequest("Email and password are required.");

            // 🔐 Generate a SecurityStamp
            string securityStamp = Guid.NewGuid().ToString();

            // 🧭 Map + hash the password
            User user;
            try
            {
                user = userDto.MapToUserEntity(_passwordHasher.HashPassword, securityStamp);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            // 📦 Registration
            var result = await _userService.RegisterUserAsync(user);

            if (!result)
                return Conflict("Email already exists or registration failed.");

            return Ok("User registered successfully.");
        }
        [HttpPost("verifytoken")]
        public IActionResult VerifyToken()
        {
            var accessToken = Request.Cookies["AccessToken"];

            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("🔒 AccessToken manquant dans les cookies.");
                return Unauthorized(new { message = "Token absent." });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _tokenGenerator.GetSecretKey(); // méthode à ajouter dans TokenGenerator pour exposer la clé

            try
            {
                var principal = tokenHandler.ValidateToken(accessToken, new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.Zero // éviter la tolérance de 5 min par défaut
                }, out var validatedToken);

                var jwtToken = validatedToken as JwtSecurityToken;
                if (jwtToken == null)
                {
                    _logger.LogWarning("🔒 Token invalide (pas un JWT).");
                    return Unauthorized(new { message = "Token non valide." });
                }

                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                var role = principal.FindFirst(ClaimTypes.Role)?.Value;

                if (email == null || role == null)
                {
                    _logger.LogWarning("🔒 Claims manquants dans le token.");
                    return Unauthorized(new { message = "Token incomplet." });
                }

                _logger.LogInformation("✅ Token valide pour {Email}, rôle {Role}", email, role);

                return Ok(new
                {
                    message = "Token valide.",
                    email,
                    role
                });
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("⌛ Token expiré.");
                return Unauthorized(new { message = "Token expiré." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("❌ Erreur lors de la validation du token : {Message}", ex.Message);
                return Unauthorized(new { message = "Token invalide." });
            }
        }
#if DEBUG
        [HttpPost("dev-login")]
        public async Task<IActionResult> DevLogin()
        {
            var email = "exemple@exemple.com";
            var role = Role.Admin;

            // Vérifie si l'utilisateur existe
            var user = await _userService.GetUserByEmailAsync(email);
            if (user == null)
            {
                var securityStamp = Guid.NewGuid().ToString();
                var passwordHash = _passwordHasher.HashPassword("Test1234=", securityStamp);

                user = new User
                {
                    Email = email,
                    Role = role,
                    PasswordHash = passwordHash,
                    SecurityStamp = securityStamp,
                    Status = Status.Active
                };

                user.Activate(); // si tu as cette méthode

                await _userService.RegisterUserAsync(user); // ✅ méthode correcte avec 1 seul paramètre
            }

            // Authentifie avec JWT
            var accessToken = _tokenGenerator.GenerateToken(email, role);
            var refreshToken = await _refreshTokenService.GenerateAsync(email);

            HttpContext.Response.Cookies.Append("AccessToken", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(30)
            });

            HttpContext.Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(1)
            });

            _logger.LogInformation("🚀 Dev auto-login as admin : {Email}", email);

            return Ok(new
            {
                message = "Auto-login as DEV admin successful",
                email,
                role = role.ToString()
            });
        }
#endif
    }
}


















































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.