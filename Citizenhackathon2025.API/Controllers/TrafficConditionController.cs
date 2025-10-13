using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using HubEvents = CitizenHackathon2025.Shared.StaticConfig.Constants.TrafficConditionHubMethods;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [Route("api/[controller]")]
    [ApiController]
    public class TrafficConditionController : ControllerBase
    {
        private readonly ITrafficConditionRepository _trafficConditionRepository;
        private readonly ITrafficApiService _trafficApiService;
        private readonly IHubContext<TrafficHub> _hubContext;

        private const string HubMethod_ReceiveTrafficConditionUpdate = "ReceiveTrafficConditionUpdate";
        public TrafficConditionController(ITrafficConditionRepository trafficConditionRepository, ITrafficApiService trafficApiService, IHubContext<TrafficHub> hubContext)
        {
            _trafficConditionRepository = trafficConditionRepository;
            _trafficApiService = trafficApiService;
            _hubContext = hubContext;
        }
        // 1) Endpoint to retrieve the latest in the database
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestTrafficCondition(CancellationToken ct)
        {
            var trafficConditions = await _trafficConditionRepository.GetLatestTrafficConditionAsync(limit: 10, ct: ct);
            var dtos = trafficConditions.Select(tc => tc?.MapToTrafficConditionDTO()).ToList();
            return Ok(dtos);
        }
        // 2) Endpoint for live fetch from Waze
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent(double lat, double lon)
        {
            var dto = await _trafficApiService.GetCurrentTrafficAsync(lat, lon);
            if (dto == null) return NotFound();

            // Mapper DTO → Entity
            var entity = new TrafficCondition
            {
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                DateCondition = dto.DateCondition,
                CongestionLevel = dto.CongestionLevel,
                IncidentType = dto.IncidentType
            };
            // Backup to database and distribution
            var saved = await _trafficConditionRepository.SaveTrafficConditionAsync(entity);
            await _hubContext.Clients.All.SendAsync("NewTrafficCondition", saved);
            return Ok(saved);
        }
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTrafficConditionById(int id)
        {
            if (id <= 0) return BadRequest("The provided ID is invalid.");
            var trafficCondition = await _trafficConditionRepository.GetByIdAsync(id);
            if (trafficCondition == null || !trafficCondition.Active)
            {
                return NotFound($"TrafficCondition with ID {id} not found or inactive.");
            }

            return Ok(trafficCondition.MapToTrafficConditionDTO());
        }
        [HttpGet("test-di")]
        public IActionResult TestDi()
        {
            return Ok(_trafficApiService.GetType().Name); // Must display "TrafficApiService"
        }
        [HttpGet("test-getbyid/{id}")]
        public async Task<IActionResult> TestGetById([FromServices] ITrafficConditionService service, int id)
        {
            var tc = await service.GetByIdAsync(id);
            return tc == null ? NotFound() : Ok(tc);
        }
        [HttpPost]
        public async Task<IActionResult> SaveTrafficCondition([FromBody] TrafficConditionDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var entity = dto.MapToEntity();                // ❌ no Id/Active set here
            var saved = await _trafficConditionRepository.SaveTrafficConditionAsync(entity);

            if (saved is null) return Problem("Insert failed");
            await _hubContext.Clients.All.SendAsync("ReceiveTrafficConditionUpdate", saved);
            return Ok(saved.MapToTrafficConditionDTO());
        }
        [HttpPut("{id:int}")]
        public IActionResult UpdateTrafficCondition(int id, [FromBody] TrafficConditionUpdateDTO dto)
        {
            if (id != dto.Id) return BadRequest("Id mismatch");

            var entity = dto.MapToEntity();                // ✅ WithId(dto.Id)
            var result = _trafficConditionRepository.UpdateTrafficCondition(entity);

            return result != null ? Ok(result) : NotFound();
        }
    }
}












































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.