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
    #nullable disable
        private readonly IAIService _aiService;

        public TourismController(IAIService aiService)
        {
            _aiService = aiService;
        }
        [HttpPost("suggest")]
        public async Task<IActionResult> GetSuggestions([FromBody] TouristicPromptDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Prompt))
                return BadRequest("The prompt cannot be empty.");

            var response = await _aiService.GetTouristicSuggestionsAsync(dto.Prompt);

            return Ok(new { suggestions = response });
        }
    }
}











































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.