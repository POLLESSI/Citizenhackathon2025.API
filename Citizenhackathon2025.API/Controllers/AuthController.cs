using CitizenHackathon2025.API.Tools; 
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

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

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(email))
            {
                return Unauthorized();
            }

            var roles = User.FindAll(ClaimTypes.Role).Select(x => x.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            return Ok(new
            {
                IsAuthenticated = true,
                Email = email,
                Roles = roles
            });
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
            Response.Cookies.Append(Cookies.JwtTokenName, accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                Path = "/"
            });
            _logger.LogInformation("User {Email} logged in successfully", user.Email);
            return Ok(new{AccessToken = accessToken, RefreshToken = refreshToken.Token});
        }

        // -----------------------------
        // LOGOUT
        // -----------------------------
        public sealed class LogoutDTO { public string RefreshToken { get; init; } = ""; }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutSessionRequest request)
        {
            if (request is not null && !string.IsNullOrWhiteSpace(request.Email) && !string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                try
                {
                    await _refreshTokenService.InvalidateAsync(request.RefreshToken.Trim(), request.Email.Trim());
                }
                catch (Exception ex)
                {
                    /*
                     * Don't expose whether a refresh token
                     * exists.
                     */
                    _logger.LogWarning(ex, "Refresh-token revocation failed during logout.");
                }
            }

            Response.Cookies.Delete( Cookies.JwtTokenName,
                new CookieOptions
                {
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/"
                });

            return Ok(
                new
                {
                    Message = "Logged out."
                });
        }

        // -----------------------------
        // REGISTER
        // -----------------------------
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var email = dto.Email.Trim();

            var existing = await _userService.GetUserByEmailAsync(email);

            if (existing is not null)
            {
                return Conflict(new
                {
                    Message = "If registration is possible for this address,\r\nthe appropriate instructions have been processed."
                });
            }

            /*
             * SECURITY:
             *
             * A public registration can NEVER decide
             * its own role.
             */
            var userDto = await _userService.RegisterUserAsync(email, dto.Password, UserRole.User);

            _logger.LogInformation("New user registered.");

            return StatusCode(StatusCodes.Status201Created, userDto);
        }

        // -----------------------------
        // REFRESH
        // -----------------------------
        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshSessionRequest request)
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Unauthorized(
                    new
                    {
                        Message = "Invalid or expired session."
                    });
            }

            var email = request.Email.Trim();

            var refreshToken = request.RefreshToken.Trim();

            // ---------------------------------------------------------
            // 1. Validate current refresh token
            // ---------------------------------------------------------

            var valid = await _refreshTokenService.ValidateAsync(refreshToken, email);

            if (!valid)
            {
                _logger.LogWarning("Refresh token rejected for {Email}", email);

                return Unauthorized(
                    new
                    {
                        Message = "Invalid or expired session."
                    });
            }

            // ---------------------------------------------------------
            // 2. User must still be authorized to use OutZen
            // ---------------------------------------------------------

            var user = await _userService.GetUserByEmailAsync(email);

            if (user is null || !user.Active || user.Status != UserStatus.Active)
            {
                /*
                 * Revoke the token if the account
                 * became unavailable.
                 */
                await _refreshTokenService.InvalidateAsync(refreshToken, email);

                return Unauthorized(
                    new
                    {
                        Message = "Invalid or expired session."
                    });
            }

            // ---------------------------------------------------------
            // 3. Consume previous refresh token
            // ---------------------------------------------------------

            await _refreshTokenService.InvalidateAsync(refreshToken, email);
                    
            // ---------------------------------------------------------
            // 4. Generate a new pair
            // ---------------------------------------------------------

            var newAccessToken = _tokenGenerator.GenerateToken(user.Email, user.Role);

            var newRefreshToken = await _refreshTokenService.GenerateAsync(user.Email);

            // ---------------------------------------------------------
            // 5. Session tracking
            // ---------------------------------------------------------

            try
            {
                await _userSessionService.TrackAccessTokenAsync(newAccessToken, user.Email, SessionSource.Api, HttpContext);
            }
            catch (Exception ex)
            {
                /*
                 * Tracking failure does NOT invalidate
                 * an otherwise successful refresh.
                 */
                _logger.LogError(ex, "Session tracking failed at refresh for {Email}", user.Email);
            }

            _logger.LogInformation("Access token refreshed for {Email}", user.Email);

            // ---------------------------------------------------------
            // 6. Return rotated pair
            // ---------------------------------------------------------

            return Ok(
                new TokenPairResponse
                {
                    AccessToken = newAccessToken,

                    RefreshToken = newRefreshToken.Token
                });
        }
    }
}





























































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.