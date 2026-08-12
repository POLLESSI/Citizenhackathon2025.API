using CitizenHackathon2025.EmergencyIntelligence.Interfaces;
using CitizenHackathon2025.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CitizenHackathon2025.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmergencyAlertsController : ControllerBase
    {
        private readonly IEmergencyAlertRepository _repository;
        public EmergencyAlertsController(IEmergencyAlertRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive(CancellationToken ct)
        {
            var alerts = await _repository.GetActiveAsync(ct);

            return Ok(alerts.Select(EmergencyAlertDtoMapper.ToSignalRDto));
        }
    }
}
