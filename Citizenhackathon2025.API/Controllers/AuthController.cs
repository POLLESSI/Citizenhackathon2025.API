using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Enums;
using Citizenhackathon2025.Infrastructure.Services;
using Citizenhackathon2025.Shared.DTOs;
using CitizenHackathon2025.Shared.DTOs;
using CityzenHackathon2025.API.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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

        public AuthController(IUserService userService, ILogger<AuthController> logger, TokenGenerator tokenGenerator, IRefreshTokenService refreshTokenService)
        {
            _userService = userService;
            _logger = logger;
            _tokenGenerator = tokenGenerator;
            _refreshTokenService = refreshTokenService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _userService.LoginAsync(loginDto);
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
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var role = dto.Role ?? Role.User;
            var user = new User
            {
                Email = dto.Email,
                Role = role
            };

            var result = await _userService.RegisterUserAsync(dto.Email, dto.Password, role);
            if (!result)
            {
                _logger.LogWarning("⚠️ Registration failed for email: {Email}", dto.Email);
                return Conflict("User already exists.");
            }

            _logger.LogInformation("✅ Registration successful for email: {Email}", dto.Email);
            return Ok("Registration successful.");
        }
#if DEBUG
        [HttpPost("dev-login")]
        public async Task<IActionResult> DevLogin()
        {
            // ⚠️ Never enable this in production
            var email = "exemple@exemple.com"; // Fictitious or real email depending on your setup
            var role = Role.Admin;

            // Optional: Make sure this user exists
            var user = await _userService.GetUserByEmailAsync(email);
            if (user == null)
            {
                await _userService.RegisterUserAsync(email, "Test1234=", role);
            }

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
