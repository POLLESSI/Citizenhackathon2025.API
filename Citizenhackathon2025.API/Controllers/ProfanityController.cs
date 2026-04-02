using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenHackathon2025.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "Modo")]
    public sealed class ProfanityController : ControllerBase
    {
        private readonly IProfanityAdminService _service;

        public ProfanityController(IProfanityAdminService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            var items = await _service.GetPagedAsync(page, pageSize, ct);
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var item = await _service.GetByIdAsync(id, ct);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProfanityWord entity, CancellationToken ct = default)
        {
            var created = await _service.CreateAsync(entity, ct);
            return Ok(created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProfanityWord entity, CancellationToken ct = default)
        {
            entity.Id = id;
            var ok = await _service.UpdateAsync(entity, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            var ok = await _service.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpPatch("{id:int}/active")]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool active, CancellationToken ct = default)
        {
            var ok = await _service.SetActiveAsync(id, active, ct);
            return ok ? NoContent() : NotFound();
        }
    }
}