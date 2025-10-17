using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Enums;
using CitizenHackathon2025.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenHackathon2025.API.Controllers
{
    [ApiController]
    [Route("api/crowd")]
    // n dev, you can allow everything :
    // [AllowAnonymous]
    // In production, protected admin :
    [Authorize(Policy = "Admin")]
    public class CrowdCalendarController : ControllerBase
    {
        private readonly ICrowdCalendarRepository _repo;
        private readonly ICrowdAdvisoryService _advisories;

        public CrowdCalendarController(ICrowdCalendarRepository repo, ICrowdAdvisoryService advisories)
        {
            _repo = repo;
            _advisories = advisories;
        }
        // GET api/crowd/calendar/all
        [HttpGet("calendar/all")]
        #if DEBUG
        [AllowAnonymous]
        #endif
        public async Task<IActionResult> GetAll()
        {
            var items = await _repo.ListAsync(
                fromUtc: null,
                toUtc: null,
                region: null,
                placeId: null,
                active: null   
            );

            return Ok(items);
        }

        // ---- Advisory “read-only”  ----
        [HttpGet("advisories")]
        [AllowAnonymous] // optional in dev
        public async Task<IActionResult> GetAdvisories([FromQuery] string region, [FromQuery] int? placeId = null)
        {
            if (string.IsNullOrWhiteSpace(region)) return BadRequest("Missing region.");
            var list = await _advisories.GetAdvisoriesForTodayAsync(region, placeId);
            return Ok(list);
        }

        // ---- LIST ----
        [HttpGet("calendar")]
        public async Task<IActionResult> List([FromQuery] DateTime? from = null,
                                              [FromQuery] DateTime? to = null,
                                              [FromQuery] string? region = null,
                                              [FromQuery] int? placeId = null,
                                              [FromQuery] bool? active = true)
        {
            var items = await _repo.ListAsync(from, to, region, placeId, active);
            return Ok(items);
        }

        // ---- GET BY ID ----
        [HttpGet("calendar/{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var item = await _repo.GetByIdAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        // ---- CREATE ----
        [HttpPost("calendar")]
        public async Task<IActionResult> Create([FromBody] CrowdCalendarEntry e)
        {
            if (e is null) return BadRequest();
            // normalize enums and fields
            e.ExpectedLevel = (CrowdLevelEnum)Math.Clamp((int)e.ExpectedLevel, 1, 4);
            var n = await _repo.InsertAsync(e);
            return n > 0 ? CreatedAtAction(nameof(GetById), new { id = e.Id }, e) : Problem("Insert failed");
        }

        // ---- UPDATE ----
        [HttpPut("calendar/{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] CrowdCalendarEntry e)
        {
            if (e is null || id != e.Id) return BadRequest("Id mismatch.");
            var n = await _repo.UpdateAsync(e);
            return n > 0 ? NoContent() : NotFound();
        }

        // ---- UPSERT ----
        [HttpPost("calendar/upsert")]
        public async Task<IActionResult> Upsert([FromBody] CrowdCalendarEntry e)
        {
            if (e is null) return BadRequest();
            var n = await _repo.UpsertAsync(e);
            return n > 0 ? Ok(e) : Problem("Upsert failed");
        }

        // ---- SOFT DELETE ----
        [HttpDelete("calendar/{id:int}")]
        public async Task<IActionResult> SoftDelete([FromRoute] int id)
        {
            var n = await _repo.SoftDeleteAsync(id);
            return n > 0 ? NoContent() : NotFound();
        }

        // ---- RESTORE ----
        [HttpPost("calendar/{id:int}/restore")]
        public async Task<IActionResult> Restore([FromRoute] int id)
        {
            var n = await _repo.RestoreAsync(id);
            return n > 0 ? NoContent() : NotFound();
        }

        // ---- HARD DELETE (optional) ----
        [HttpDelete("calendar/{id:int}/hard")]
        public async Task<IActionResult> HardDelete([FromRoute] int id)
        {
            var n = await _repo.HardDeleteAsync(id);
            return n > 0 ? NoContent() : NotFound();
        }
    }
}











































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.