#if DEBUG
using CitizenHackathon2025.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CitizenHackathon2025.API.Controllers.Diagnostics
{
    [ApiController]
    [Route("api/diagnostics/ollama")]
    public sealed class OllamaDiagnosticsController : ControllerBase
    {
        private readonly IGenerativeAiService _ai;

        public OllamaDiagnosticsController(IGenerativeAiService ai)
        {
            _ai = ai;
        }

        [HttpPost("ping")]
        public async Task<IActionResult> Ping(CancellationToken ct)
        {
            var result = await _ai.GenerateTextAsync(
                "Réponds uniquement par OK.",
                ct);

            return Ok(new
            {
                Ok = true,
                Response = result
            });
        }
    }
}
#endif



























































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.