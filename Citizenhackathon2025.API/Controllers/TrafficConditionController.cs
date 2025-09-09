using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrafficConditionController : ControllerBase
    {
        private readonly ITrafficConditionRepository _trafficConditionRepository;
        private readonly ITrafficApiService _trafficApiService;
        private readonly IHubContext<TrafficHub> _hubContext;

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
            var trafficConditions = await _trafficConditionRepository.GetLatestTrafficConditionAsync(ct);
            return Ok(trafficConditions);
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
            // Sauvegarde en base et diffusion
            var saved = await _trafficConditionRepository.SaveTrafficConditionAsync(entity);
            await _hubContext.Clients.All.SendAsync("NewTrafficCondition", saved);
            return Ok(saved);
        }
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTrafficConditionById(int id)
        {
            var trafficCondition = await _trafficConditionRepository.GetByIdAsync(id);
            if (trafficCondition == null || !trafficCondition.Active)
            {
                return NotFound($"TrafficCondition with ID {id} not found or inactive.");
            }

            return Ok(trafficCondition);
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
        public async Task<IActionResult> SaveTrafficCondition([FromBody] TrafficCondition @trafficCondition)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var savedTrafficCondition = await _trafficConditionRepository.SaveTrafficConditionAsync(@trafficCondition); // 👈 parameter correction

            if (savedTrafficCondition == null)
                return StatusCode(500, "Error while saving");

            // ✅ Real-time broadcasting
            await _hubContext.Clients.All.SendAsync("NewPlace", savedTrafficCondition);

            return Ok(savedTrafficCondition);
        }
        [HttpPut("{id:int}")]
        public IActionResult UpdateTrafficCondition(int id, [FromBody] TrafficConditionUpdateDTO dto)
        {
            if (id != dto.Id)
                return BadRequest("Id mismatch between URL and body");

            var entity = dto.MapToTrafficCondition();
            var result = _trafficConditionRepository.UpdateTrafficCondition(entity);

            return result != null ? Ok(result) : NotFound($"TrafficCondition with ID {id} not found.");
        }
    }
}












































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.