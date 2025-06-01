using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Hubs.Hubs;
using Citizenhackathon2025.Shared.DTOs;
using CitizenHackathon2025.Application.Interfaces;
using static Citizenhackathon2025.Application.Extensions.MapperExtensions;
using CityzenHackathon2025.API.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

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
                return StatusCode(500, "Erreur lors de l'enregistrement");

            // 👇 ici tu peux utiliser SendAsync tranquillement
            await _hubContext.Clients.All.SendAsync("NewCrowdInfo", savedCrowdInfo.MapToCrowdInfoDTO());

            return Ok(savedCrowdInfo.MapToCrowdInfoDTO());
        }
    }
}
