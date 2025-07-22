using CitizenHackathon2025.Infrastructure.Services.Monitoring;
using CitizenHackathon2025.DTOs.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace CitizenHackathon2025.API.Controllers
{
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

        [HttpGet("health")]
        public async Task<IActionResult> ReceiveCspViolation([FromBody] CspReportModel model)
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
        public IActionResult GetAllReports()
        {
            return Ok(_store.GetAll());
        }

        [HttpDelete("clear")]
        public IActionResult ClearAll()
        {
            _store.Clear();
            return NoContent();
        }
    }
}
