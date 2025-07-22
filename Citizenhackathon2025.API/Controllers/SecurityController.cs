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

        public SecurityController(ILogger<SecurityController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveCspViolation([FromBody] object report)
        {
            // You can deserialize a CSPReportModel object here if you want (see below)
            var json = await new StreamReader(Request.Body).ReadToEndAsync();
            _logger.LogWarning("CSP Violation reported: {Report}", json);
            return Ok();
        }
        public class CspReportModel
        {
            [JsonPropertyName("csp-report")]
            public CspReportContent Report { get; set; }
        }

        public class CspReportContent
        {
        #nullable disable
            public string DocumentUri { get; set; }
            public string Referrer { get; set; }
            public string ViolatedDirective { get; set; }
            public string EffectiveDirective { get; set; }
            public string OriginalPolicy { get; set; }
            public string BlockedUri { get; set; }
            public string SourceFile { get; set; }
            public int? LineNumber { get; set; }
            public int? ColumnNumber { get; set; }
            public string ScriptSample { get; set; }
        }
    }

}
