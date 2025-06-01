using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Hubs.Hubs;
using Citizenhackathon2025.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuggestionController : ControllerBase
    {
    #nullable disable
        private readonly ISuggestionRepository _suggestionRepository;
        private readonly IAIService _aiService;
        private readonly IHubContext<GPTHub> _hubContext;

        public SuggestionController(ISuggestionRepository suggestionRepository, IAIService aiService, IHubContext<GPTHub> hubContext)
        {
            _suggestionRepository = suggestionRepository;
            _aiService = aiService;
            _hubContext = hubContext;
        }
        // ✅ GET Latest
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestSuggestion()
        {
            var suggestions = await _suggestionRepository.GetLatestSuggestionAsync();
            return Ok(suggestions);
        }
        // ✅ POST classique
        [HttpPost]
        public async Task<IActionResult> SaveSuggestion([FromBody] Suggestion suggestion)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var savedSuggestion = await _suggestionRepository.SaveSuggestionAsync(suggestion);

            if (savedSuggestion == null)
                return StatusCode(500, "Error while saving");

            await _hubContext.Clients.All.SendAsync("NewSuggestion", savedSuggestion);
            return Ok(savedSuggestion);
        }
        // ✅ POST AI generation + recording + SignalR
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateSuggestion([FromBody] WeatherForecastSuggestionDTO forecastDto)
        {
            var prompt = $"He does {forecastDto.TemperatureC}°C with {forecastDto.Humidity}% humidity to {forecastDto.Location}. " +
                         $"Offers a pleasant alternative activity or location for locals, with a concise and engaging tone.";

            // Fix for CS0815: The issue occurs because the method `GetSuggestionsAsync` in `IAIService` is defined to return `Task` (void), 
            // but the code is trying to assign its result to a variable. The method should return a `Task<string>` instead.

            var gptResponse = await _aiService.GetSuggestionsAsync(prompt);

            var newSuggestion = new Suggestion
            {
                UserId = 1, // ou via ClaimsPrincipal si auth
                DateSuggestion = DateTime.UtcNow,
                OriginalPlace = forecastDto.Location,
                SuggestedAlternatives = gptResponse,
                Reason = "AI-generated based on weather"
            };

            var savedSuggestion = await _suggestionRepository.SaveSuggestionAsync(newSuggestion);

            await _hubContext.Clients.All.SendAsync("NewSuggestion", savedSuggestion);

            return Ok(new { Suggestion = savedSuggestion });
        }
        // ✅ POST text summary
        [HttpPost("summarize")]
        public async Task<IActionResult> SummarizeText([FromBody] string inputText)
        {
            if (string.IsNullOrWhiteSpace(inputText))
                return BadRequest("Empty text");

            try
            {
                var summary = await _aiService.SummarizeTextAsync(inputText);
                return Ok(new { Summary = summary });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"GPT error while summarizing : {ex.Message}");
            }
        }
        // ✅ POST translation into French
        [HttpPost("translate")]
        public async Task<IActionResult> TranslateToFrench([FromBody] string englishText)
        {
            if (string.IsNullOrWhiteSpace(englishText))
                return BadRequest("Empty text");

            try
            {
                var translated = await _aiService.TranslateToFrenchAsync(englishText);
                return Ok(new { Translation = translated });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"GPT error while translating : {ex.Message}");
            }
        }
        // ✅ POST translation into Dutch
        [HttpPost("translate/dutch")]
        public async Task<IActionResult> TranslateToDutchAsync([FromBody] string englishText)
        {
            if (string.IsNullOrWhiteSpace(englishText))
                return BadRequest("Empty text");

            try
            {
                var translated = await _aiService.TranslateToDutchAsync(englishText);
                return Ok(new { Translation = translated });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"GPT error while translating : {ex.Message}");
            }
        }
        // ✅ POST translation into Dutch
        [HttpPost("translate/german")]
        public async Task<IActionResult> TranslateToGermanAsync([FromBody] string englishText)
        {
            if (string.IsNullOrWhiteSpace(englishText))
                return BadRequest("Empty text");

            try
            {
                var translated = await _aiService.TranslateToGermanAsync(englishText);
                return Ok(new { Translation = translated });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"GPT error while translating : {ex.Message}");
            }
        }
    }
}
