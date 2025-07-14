using CitizeHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Application.Extensions;
using Citizenhackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Enums;
using Citizenhackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Shared.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IHubContext<UserHub> _hubContext;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, IHubContext<UserHub> hubContext, ILogger<UserController> logger)
        {
            _userService = userService;
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            var userDto = await _userService.RegisterUserAsync(dto.Email, dto.Password, UserRole.User);
            await _hubContext.Clients.All.SendAsync("UserRegistered", userDto.Email);
            return Ok(userDto);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            if (!await _userService.LoginAsync(dto.Email, dto.Password))
                return Unauthorized("Invalid credentials");

            var user = await _userService.GetUserByEmailAsync(dto.Email);
            await _hubContext.Clients.All.SendAsync("UserLogged", user.Email);
            return Ok("Login successful");
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
        public IActionResult Update([FromBody] User user)
        {
            var updated = _userService.UpdateUser(user);
            return updated != null ? Ok(updated) : BadRequest("Update failed");
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