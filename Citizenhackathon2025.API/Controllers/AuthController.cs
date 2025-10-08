using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Enums;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.API.Tools; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            if (string.IsNullOrEmpty(token))
                return BadRequest("Missing token");

            await _refreshTokenService.InvalidateAsync(token);
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
            var isValid = await _refreshTokenService.ValidateAsync(request.RefreshToken);
            if (!isValid)
                return Unauthorized(new { Message = "Invalid or expired refresh token" });

            var user = await _userService.GetUserByIdAsync(request.UserId);
            if (user == null)
                return Unauthorized(new { Message = "User not found" });

            var newAccessToken = _tokenGenerator.GenerateToken(user.Email, user.Role);
            var newRefreshToken = await _refreshTokenService.GenerateAsync(user.Email);

            await _refreshTokenService.InvalidateAsync(request.RefreshToken);

            return Ok(new
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }
    }
}


















































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.