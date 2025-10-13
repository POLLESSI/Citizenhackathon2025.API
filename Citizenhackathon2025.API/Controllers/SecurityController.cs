using CitizenHackathon2025.Infrastructure.Services.Monitoring;
using CitizenHackathon2025.Shared.Security;
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
        private readonly CspViolationStore _store;

        public SecurityController(ILogger<SecurityController> logger, CspViolationStore store)
        {
            _logger = logger;
            _store = store;
        }

        // GET /csp-report/health  ✅ no body on a GET
        [HttpGet("health")]
        public IActionResult Health() => Ok(new { status = "ok" });

        // POST /csp-report        ✅ the spec expects a POST with body
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

        [HttpGet("all")]
        public IActionResult GetAllReports() => Ok(_store.GetAll());

        [HttpDelete("clear")]
        public IActionResult ClearAll() { _store.Clear(); return NoContent(); }
    }
}
