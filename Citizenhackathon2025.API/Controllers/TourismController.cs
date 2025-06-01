using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Shared.DTOs;
using CitizenHackathon2025.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;


namespace CitizenHackathon2025.API.Controllers
{
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
