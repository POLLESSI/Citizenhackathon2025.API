using System.Security.Claims;
using CitizenHackathon2025.API.Tools; 
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Enums;
using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;
        private readonly TokenGenerator _tokenGenerator;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IUserSessionService _userSessionService;
        private static string? GetEmail(ClaimsPrincipal p) =>
            p?.FindFirst(ClaimTypes.Email)?.Value ?? p?.Identity?.Name;
        public AuthController(IUserService userService, ILogger<AuthController> logger, TokenGenerator tokenGenerator, IRefreshTokenService refreshTokenService,IUserSessionService userSessionService)                  
        {
            _userService = userService;
            _logger = logger;
            _tokenGenerator = tokenGenerator;
            _refreshTokenService = refreshTokenService;
            _userSessionService = userSessionService;               
        }

        // -----------------------------
        // LOGIN
        // -----------------------------
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO request)
        {
            var user = await _userService.AuthenticateAsync(request.Email, request.Password);
            if (user is null)
            {
                _logger.LogWarning("Login attempt failed for {Email}", request.Email);
                return Unauthorized(new { Message = "Invalid credentials" });
            }

            var accessToken = _tokenGenerator.GenerateToken(user.Email, user.Role);
            var refreshToken = await _refreshTokenService.GenerateAsync(user.Email);

            // ---- SESSION TRACKING ----
            try
            {
                await _userSessionService.TrackAccessTokenAsync(
                    accessToken, user.Email, SessionSource.Api, HttpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session tracking failed at login for {Email}", user.Email);
                // The login does not fail.
            }

            _logger.LogInformation("User {Email} logged in successfully", user.Email);
            return Ok(new { AccessToken = accessToken, RefreshToken = refreshToken });
        }

        // -----------------------------
        // LOGOUT
        // -----------------------------
        public sealed class LogoutDTO { public string RefreshToken { get; init; } = ""; }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken)) return BadRequest("Missing token");
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return Unauthorized(new { Message = "No email in principal" });

            await _refreshTokenService.InvalidateAsync(dto.RefreshToken, email);
            return Ok(new { message = "Logged out successfully" });
        }

        // -----------------------------
        // REGISTER
        // -----------------------------
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            var userDto = await _userService.RegisterUserAsync(dto.Email, dto.Password, dto.Role);
            _logger.LogInformation("New registered user : {Email}", dto.Email);
            return Ok(userDto);
        }

        // -----------------------------
        // REFRESH
        // -----------------------------
        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshDTO request)
        {
            var user = await _userService.GetUserByIdAsync(request.UserId);
            if (user is null) return Unauthorized(new { Message = "User not found" });

            if (!await _refreshTokenService.ValidateAsync(request.RefreshToken, user.Email))
                return Unauthorized(new { Message = "Invalid or expired refresh token" });

            var newAccess = _tokenGenerator.GenerateToken(user.Email, user.Role);
            var newRefresh = await _refreshTokenService.GenerateAsync(user.Email);
            await _refreshTokenService.InvalidateAsync(request.RefreshToken, user.Email);

            // ---- SESSION TRACKING (new JWT session) ----
            try
            {
                await _userSessionService.TrackAccessTokenAsync(
                    newAccess, user.Email, SessionSource.Api, HttpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session tracking failed at refresh for {Email}", user.Email);
            }

            return Ok(new { AccessToken = newAccess, RefreshToken = newRefresh });
        }
    }
}





























































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.