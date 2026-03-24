using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [Route("api/[controller]")]
    [ApiController]
    public class TourismController : ControllerBase
    {
        private readonly IGenerativeAiService _ai;

        public TourismController(IGenerativeAiService ai)
        {
            _ai = ai;
        }

        [HttpPost("suggest")]
        public async Task<IActionResult> GetSuggestions([FromBody] TouristicPromptDTO dto, CancellationToken ct)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Prompt))
                return BadRequest("The prompt cannot be empty.");

            var response = await _ai.GenerateTextAsync(dto.Prompt, ct);

            return Ok(new { suggestions = response });
        }
    }
}
















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.