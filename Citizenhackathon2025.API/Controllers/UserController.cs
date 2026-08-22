using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CitizenHackathon2025.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("per-user")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserHubService _userHubService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, IUserHubService userHubService, ILogger<UserController> logger)
        {
            _userService = userService;
            _userHubService = userHubService;
            _logger = logger;
        }

        // =========================================================
        // ADMIN / MODERATOR
        // =========================================================

        [Authorize(Policy = "AdminOrModo")]
        [HttpGet("active")]
        public async Task<IActionResult> GetAllActive()
        {
            var users = await _userService.GetAllActiveUsersAsync();

            var result = users.Select(x => x.ToPublicDTO()).ToList();

            return Ok(result);
        }

        [Authorize(Policy = "AdminOrModo")]
        [HttpGet("getbyemail/{email}")]
        public async Task<IActionResult> GetByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest();

            var user = await _userService.GetUserByEmailAsync(email.Trim());

            if (user is null)
                return NotFound();

            return Ok(user.ToPublicDTO());
        }

        [Authorize(Policy = "AdminOrModo")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest();

            var user = await _userService.GetUserByIdAsync(id);

            if (user is null)
                return NotFound();

            return Ok(user.ToPublicDTO());
        }

        [Authorize(Policy = "AdminOrModo")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest();

            await _userService.DeactivateUserAsync(id);

            return NoContent();
        }

        // =========================================================
        // ROLE
        // =========================================================

        [Authorize(Policy = Policies.AdminPolicy)]
        [HttpPatch("role/{id:int}")]
        public IActionResult SetRole(int id, [FromQuery] string newRole)
        {
            if (id <= 0)
                return BadRequest();

            if (string.IsNullOrWhiteSpace(newRole))
                return BadRequest("Role is required.");

            if (!Enum.TryParse<UserRole>(newRole, ignoreCase: true, out _))
            {
                return BadRequest($"Invalid role '{newRole}'.");
            }

            _userService.SetRole(id, newRole);

            return NoContent();
        }
    }
}





































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.