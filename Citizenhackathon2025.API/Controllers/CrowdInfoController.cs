using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Application.Interfaces;
using CityzenHackathon2025.API.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using static CitizenHackathon2025.Application.Extensions.MapperExtensions;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.API.Controllers
{
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

            var result = crowdInfos.Select(c => c.MapToCrowdInfoDTO());

            return Ok(result);
        }
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCrowdInfoById(int id)
        {
            if (id <= 0)
                return BadRequest("The provided ID is invalid.");

            var crowdInfo = await _crowdInfoRepository.GetCrowdInfoByIdAsync(id);

            if (crowdInfo == null)
                return NotFound($"No traffic data found for the identifier {id}.");

            return Ok(crowdInfo.MapToCrowdInfoDTO());
        }
        [HttpGet("by-location")]
        public async Task<IActionResult> GetByLocation([FromQuery] string locationName)
        {
            var all = await _crowdInfoRepository.GetAllCrowdInfoAsync();
            var filtered = all.Where(c => c.LocationName.Equals(locationName, StringComparison.OrdinalIgnoreCase));

            return Ok(filtered.Select(c => c.MapToCrowdInfoDTO()));
        }
        [HttpPost]
        public async Task<IActionResult> SaveCrowdInfo([FromBody] CrowdInfoDTO crowdInfoDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var crowdInfo = crowdInfoDTO
                .MapToCrowdInfoWithTimestamp()
                .MapToCrowdInfo();
            var savedCrowdInfo = await _crowdInfoRepository.SaveCrowdInfoAsync(crowdInfo);

            if (savedCrowdInfo == null)
                return StatusCode(500, "Error while saving");

            // 👇 ici tu peux utiliser SendAsync tranquillement
            await _hubContext.Clients.All.SendAsync("NewCrowdInfo", savedCrowdInfo.MapToCrowdInfoDTO());

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

            // Optional: Notify SignalR clients if needed
            await _hubContext.Clients.All.SendAsync("CrowdInfoArchived", id);

            return Ok(new { Message = $"Attendance data with ID {id} successfully archived." });
        }
        [HttpPut("update")]
        public IActionResult UpdateCrowdInfo([FromBody] CrowdInfo crowdInfo)
        {
            var result = _crowdInfoRepository.UpdateCrowdInfo(crowdInfo);
            return result != null ? Ok(result) : NotFound();
        }
    }
}











































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.