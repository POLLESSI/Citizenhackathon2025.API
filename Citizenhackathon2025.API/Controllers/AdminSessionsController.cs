using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CitizenHackathon2025.API.Controllers
{
    [ApiController]
    [Route("api/admin/sessions")]
    [Authorize(Policy = "Admin")]
    public class AdminSessionsController : ControllerBase
    {
        private readonly IUserSessionRepository _repo;
        public AdminSessionsController(IUserSessionRepository repo) => _repo = repo;

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? email, [FromQuery] bool onlyActive = true)
        {
            var list = await _repo.QueryAsync(new SessionQuery { Email = email, OnlyActive = onlyActive });
            return Ok(list.Select(s => new {
                s.UserEmail,
                s.Jti,
                s.Source,
                s.IssuedAtUtc,
                s.ExpiresAtUtc,
                s.LastSeenUtc,
                s.Ip,
                s.UserAgent,
                s.IsRevoked
            }));
        }

        [HttpPost("{jti}/revoke")]
        public async Task<IActionResult> Revoke(string jti)
        {
            var n = await _repo.RevokeAsync(jti, "admin");
            return n > 0 ? Ok() : NotFound();
        }
    }
}
