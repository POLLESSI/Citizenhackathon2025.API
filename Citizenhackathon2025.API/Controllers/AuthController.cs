using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Enums;
using Citizenhackathon2025.Infrastructure.Services;
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
using CitizenHackathon2025.DTOs.DTOs;
using Citizenhackathon2025.Application.Interfaces;

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
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokenGenerator = tokenGenerator;
            _refreshTokenService = refreshTokenService;
            _passwordHasher = passwordHasher;
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            if (!await _userService.LoginAsync(dto.Email, dto.Password))
                return Unauthorized("Invalid credentials");

            var user = await _userService.GetUserByEmailAsync(dto.Email);
            var token = _tokenGenerator.GenerateToken(user.Email, user.Role);
            return Ok(new { Token = token });
        }

        [HttpPost("refresh")]
        public IActionResult Refresh()
        {
            // Récupère claims (après validation du token existant)
            var email = User.FindFirstValue(ClaimTypes.Email);
            var roleStr = User.FindFirstValue(ClaimTypes.Role);
            if (email == null || roleStr == null)
                return Unauthorized();

            var role = RoleExtensions.ParseRole(roleStr);
            var token = _tokenGenerator.GenerateToken(email, role);
            return Ok(new { Token = token });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            var userDto = await _userService.RegisterUserAsync(dto.Email, dto.Password, UserRole.User);
            return Ok(userDto);
        }

        [HttpPost("dev-login")]
        public async Task<IActionResult> DevLogin()
        {
            var email = "exemple@exemple.com";
            var role = UserRole.Admin;

            var user = await _userService.GetUserByEmailAsync(email);
            if (user == null)
            {
                // passe ici seulement en DEV
                await _userService.RegisterUserAsync(email, "Test1234=", role);
                user = await _userService.GetUserByEmailAsync(email);
            }

            var token = _tokenGenerator.GenerateToken(user.Email, user.Role);
            return Ok(new { Token = token });
        }
    }
}


















































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.