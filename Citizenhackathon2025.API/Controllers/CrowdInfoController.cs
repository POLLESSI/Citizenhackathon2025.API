using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using static CitizenHackathon2025.Application.Extensions.MapperExtensions;
using HubEvents = CitizenHackathon2025.Shared.StaticConfig.Constants.CrowdHubMethods;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [Route("api/[controller]")]
    [ApiController]
    public class CrowdInfoController : ControllerBase
    {
    #nullable disable
        private readonly ICrowdInfoRepository _crowdInfoRepository;
        private readonly IHubContext<CrowdHub> _hubContext;

        public CrowdInfoController(ICrowdInfoRepository crowdInfoRepository, IHubContext<CrowdHub> hubContext)
        {
            _crowdInfoRepository = crowdInfoRepository;
            _hubContext = hubContext;
        }
        [HttpGet("all")]
        public async Task<IActionResult> GetAllCrowdInfo()
        {
            var crowdInfos = await _crowdInfoRepository.GetAllCrowdInfoAsync();

            var result = crowdInfos.Select(c => c.MapToCrowdInfoDTO()).ToList();
            return Ok(result);
        }
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCrowdInfoById(int id)
        {
            if (id <= 0)
                return BadRequest("The provided ID is invalid.");

            var crowdInfo = await _crowdInfoRepository.GetCrowdInfoByIdAsync(id);

            if (crowdInfo == null)
                return NotFound($"No crowd infos data found for the identifier {id}.");

            return Ok(crowdInfo.MapToCrowdInfoDTO());
        }
        [HttpGet("by-location")]
        public async Task<IActionResult> GetByLocation([FromQuery] string locationName)
        {
            var all = await _crowdInfoRepository.GetAllCrowdInfoAsync();
            var filtered = all.Where(c => c.LocationName.Equals(locationName, StringComparison.OrdinalIgnoreCase));

            return Ok(filtered.Select(c => c.MapToCrowdInfoDTO()));
        }
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatest()
        {
            var all = await _crowdInfoRepository.GetAllCrowdInfoAsync();
            var latest = all.OrderByDescending(c => c.Timestamp).Take(50);
            return Ok(latest.Select(c => c.MapToCrowdInfoDTO()));
        }
        [HttpPost]
        public async Task<IActionResult> SaveCrowdInfo([FromBody] CrowdInfoDTO crowdInfoDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var crowdInfo = crowdInfoDTO.MapToCrowdInfoWithTimestamp().MapToCrowdInfo();
            var savedCrowdInfo = await _crowdInfoRepository.SaveCrowdInfoAsync(crowdInfo);

            if (savedCrowdInfo == null)
                return StatusCode(500, "Error while saving");

            // Notify clients
            await _hubContext.Clients.All.SendAsync(HubEvents.ToClient.ReceiveCrowdUpdate, savedCrowdInfo.MapToCrowdInfoDTO());

            return Ok(savedCrowdInfo.MapToCrowdInfoDTO());
        }
        [HttpDelete("archive/{id:int}")]
        public async Task<IActionResult> ArchiveCrowdInfo(int id)
        {
            if (id <= 0)
                return BadRequest("The provided ID is invalid.");

            var deleted = await _crowdInfoRepository.DeleteCrowdInfoAsync(id);

            if (!deleted)
                return NotFound($"No active attendance data found for the identifier {id}.");

            // Notify SignalR clients
            await _hubContext.Clients.All.SendAsync(CrowdHubMethods.ToClient.CrowdInfoArchived, id);

            return Ok(new { Message = $"Attendance data with ID {id} successfully archived." });
        }
        [HttpPut("update")]
        public async Task<IActionResult> UpdateCrowdInfo([FromBody] CrowdInfoDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.Id <= 0) return BadRequest("Missing or invalid Id.");

            var entity = dto.MapToCrowdInfo();
            var updated = _crowdInfoRepository.UpdateCrowdInfo(entity);
            if (updated is null) return NotFound();

            var updatedDto = updated.MapToCrowdInfoDTO();

            // real-time push, same pattern as POST
            await _hubContext.Clients.All.SendAsync(HubEvents.ToClient.ReceiveCrowdUpdate, updated.MapToCrowdInfoDTO());

            return Ok(updatedDto);
        }
    }
}











































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.