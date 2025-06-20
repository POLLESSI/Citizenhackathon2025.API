using CitizeHackathon2025.Hubs.Hubs;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Hubs.Hubs;
using Citizenhackathon2025.Shared.DTOs;
using CitizenHackathon2025.Shared.Utils;
using Citizenhackathon2025.Application.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

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
        private static byte[] HashPassword(string password, string securityStamp)
        {
            using var sha = System.Security.Cryptography.SHA512.Create();
            var salted = $"{password}:{securityStamp}";
            return sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(salted));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDTO userDto)
        {
            if (userDto == null || string.IsNullOrWhiteSpace(userDto.Email) || string.IsNullOrWhiteSpace(userDto.Pwd))
                return BadRequest("Email and password are required.");

            // 🔐 Generate a SecurityStamp
            string securityStamp = Guid.NewGuid().ToString();

            // 🧭 Mapping from DTO to entity
            User user;
            try
            {
                user = userDto.MapToUserEntity(HashPassword, securityStamp); // ✅ Extension method
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            // 📦 Register user
            var result = await _userService.RegisterUserAsync(user); // ✅ DAL method

            if (!result)
                return Conflict("Email already exists or registration failed.");

            return Ok("User registered successfully.");
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.LoginAsync(loginDto.Email, loginDto.Password);

            if (user == false)
            {
                _logger.LogWarning("❌ Invalid login for {Email}", loginDto.Email);
                return Unauthorized("Invalid credentials.");
            }

            return Ok(user);
        }
        [HttpGet("active")]
        public async Task<IActionResult> GetAllActiveUsers()
        {
            var users = await _userService.GetAllActiveUsersAsync();
            return Ok(users);
        }
        [HttpGet("getbyemail/{email}")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var user = await _userService.GetUserByEmailAsync(email);
            if (user == null)
                return NotFound("Utilisateur non trouvé.");

            return Ok(user);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            await _userService.DeactivateUserAsync(id);
            return Ok("User deactivated.");
        }
        [HttpPut("update")]
        public IActionResult UpdateUser([FromBody] User user)
        {
            var result = _userService.UpdateUser(user);
            return result != null ? Ok(result) : NotFound();
        }
        [HttpPatch("role/{id}")]
        public IActionResult SetUserRole(int id, [FromQuery] string role)
        {
            _userService.SetRole(id, role);
            return Ok("User role updated.");
        }
        
    }
}
































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.