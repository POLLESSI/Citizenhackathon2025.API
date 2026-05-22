using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Shared.Security;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [ApiController]
    [Route("csp-report")]
    public class SecurityController : ControllerBase
    {
        private readonly ILogger<SecurityController> _logger;
        private readonly ICspViolationStore _store;

        public SecurityController(ILogger<SecurityController> logger, ICspViolationStore store)
        {
            _logger = logger;
            _store = store;
        }

        [Authorize(Policy = Policies.AdminPolicy)]
        [Authorize(Policy = Policies.ModoPolicy)]
        [HttpGet("health")]
        public IActionResult Health() => Ok(new { status = "ok" });

        [Authorize(Policy = Policies.AdminPolicy)]
        [Authorize(Policy = Policies.ModoPolicy)]
        [HttpPost]
        public IActionResult ReceiveCspViolation([FromBody] CspReportModel model)
        {
            if (model?.Report is not null)
            {
                _logger.LogWarning("CSP Violation: {Directive} blocked {BlockedUri}",
                    model.Report.ViolatedDirective,
                    model.Report.BlockedUri);

                _store.Add(model.Report);
            }

            return Ok();
        }

        [Authorize(Policy = Policies.AdminPolicy)]
        [Authorize(Policy = Policies.ModoPolicy)]
        [HttpGet("all")]
        public IActionResult GetAllReports() => Ok(_store.GetAll());

        [Authorize(Policy = Policies.AdminPolicy)]
        [Authorize(Policy = Policies.ModoPolicy)]
        [HttpDelete("clear")]
        public IActionResult ClearAll()
        {
            _store.Clear();
            return NoContent();
        }
    }
}








































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.