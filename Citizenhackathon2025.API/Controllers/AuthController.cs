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
        private static string? GetEmail(ClaimsPrincipal p) =>
            p?.FindFirst(ClaimTypes.Email)?.Value ?? p?.Identity?.Name;
        public AuthController(
            IUserService userService,
            ILogger<AuthController> logger,
            TokenGenerator tokenGenerator,
            IRefreshTokenService refreshTokenService)
        {
            _userService = userService;
            _logger = logger;
            _tokenGenerator = tokenGenerator;
            _refreshTokenService = refreshTokenService;
        }

        // -----------------------------
        // LOGIN
        // -----------------------------
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO request)
        {
            var user = await _userService.AuthenticateAsync(request.Email, request.Password);

            if (user == null)
            {
                _logger.LogWarning("Login attempt failed for {Email}", request.Email);
                return Unauthorized(new { Message = "Invalid credentials" });
            }

            var accessToken = _tokenGenerator.GenerateToken(user.Email, user.Role);
            var refreshToken = await _refreshTokenService.GenerateAsync(user.Email);

            _logger.LogInformation("User {Email} logged in successfully", user.Email);

            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        // -----------------------------
        // LOGOUT
        // -----------------------------
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return BadRequest("Missing token");
            var email = GetEmail(User);
            if (string.IsNullOrWhiteSpace(email)) return Unauthorized(new { Message = "No email in principal" });

            await _refreshTokenService.InvalidateAsync(token, email);
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

            return Ok(new { AccessToken = newAccess, RefreshToken = newRefresh.Token });
        }
        private string? GetEmailFromPrincipal(ClaimsPrincipal user)
        {
            return user?.FindFirst(ClaimTypes.Email)?.Value
                ?? user?.FindFirst("email")?.Value
                ?? user?.Identity?.Name;
        }
    }
}


















































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.