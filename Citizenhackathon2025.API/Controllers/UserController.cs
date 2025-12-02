using CitizenHackathon2025.API.Tools;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IHubContext<UserHub> _hubContext;
        private readonly TokenGenerator _tokenGenerator;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, IHubContext<UserHub> hubContext, TokenGenerator tokenGenerator, ILogger<UserController> logger)
        {
            _userService = userService;
            _hubContext = hubContext;
            _tokenGenerator = tokenGenerator;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            var userDto = await _userService.RegisterUserAsync(dto.Email, dto.Password, dto.Role);
            await _hubContext.Clients.All.SendAsync("UserRegistered", userDto.Email);
            return Ok(userDto);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            if (!await _userService.LoginAsync(dto.Email, dto.Password))
                return Unauthorized("Invalid credentials");

            var user = await _userService.GetUserByEmailAsync(dto.Email);

            // ⚠️ Make sure you know the user's UserRoleS
            var token = _tokenGenerator.GenerateToken(user.Email, user.Role);

            await _hubContext.Clients.All.SendAsync("UserLogged", user.Email);
            return Ok(new { token });
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetAllActive()
        {
            var users = await _userService.GetAllActiveUsersAsync();
            return Ok(users);
        }

        [HttpGet("getbyemail/{email}")]
        public async Task<IActionResult> GetByEmail(string email)
        {
            var user = await _userService.GetUserByEmailAsync(email);
            return user == null ? NotFound() : Ok(user);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            return user == null ? NotFound() : Ok(user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _userService.DeactivateUserAsync(id);
            return NoContent();
        }

        [HttpPut("update")]
        public IActionResult Update([FromBody] UpdateUserDTO dto)
        {
            if (dto.Id <= 0) return BadRequest("Id is required.");

            var entity = new Users
            {
                Email = dto.Email,
                Role = (UserRole)dto.Role,
                Status = (UserStatus)dto.Status,
            };
            if (dto.Active) entity.Activate(); else entity.Deactivate();

            var updated = _userService.UpdateUser(new Users
            {
                // ⚠️ If you keep the setter private, change the repo signature instead.
                // to take (id, email, role, status, active) as parameters.
            });

            return updated != null ? Ok(updated) : NotFound($"User #{dto.Id} not found.");
        }

        [HttpPatch("role/{id}")]
        public IActionResult SetRole(int id, [FromQuery] string newRole)
        {
            _userService.SetRole(id, newRole);
            return Ok();
        }
    }
}





































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.