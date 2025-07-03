using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SqlServer.Dac.Model;
using MediatR;
using CitizenHackathon2025.Application.CQRS.Queries;
using CitizenHackathon2025.Application.CQRS.Commands;
using CitizenHackathon2025.DTOs.DTOs;

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
        private readonly IMediator _mediator;

        public SuggestionController(ISuggestionRepository suggestionRepository, IAIService aiService, IHubContext<GPTHub> hubContext, IMediator mediator)
        {
            _suggestionRepository = suggestionRepository;
            _aiService = aiService;
            _hubContext = hubContext;
            _mediator = mediator;
        }


        // ✅ GET: /api/Suggestion/latest
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestSuggestions()
        {
            var suggestions = await _suggestionRepository.GetLatestSuggestionAsync();
            return Ok(suggestions);
        }

        // GET: api/Suggestion/user/5
        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetSuggestionsByUser(int userId)
        {
            var query = new GetSuggestionsByUserQuery(userId);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        // DELETE: api/Suggestion/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> SoftDelete(int id)
        {
            var command = new SoftDeleteSuggestionCommand(id);
            var result = await _mediator.Send(command);

            if (!result)
                return NotFound($"Suggestion with ID {id} not found or already disabled.");

            return NoContent();
        }
        // ✅ POSTs classics
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
        [HttpPost("ai")]
        public async Task<IActionResult> GetSuggestionFromAI([FromBody] SuggestionDTO dto)
        {
            var prompt = $"I am currently at {dto.OriginalPlace} the {dto.DateSuggestion:dd/MM/yyyy}. " +
                         $"The crowd is dense and the weather is uncertain. Offer me a quiet and interesting alternative.";

            var suggestion = await _aiService.GenerateSuggestionAsync(prompt);
            return Ok(new { suggestion });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] SuggestionDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = new Suggestion
            {
                User_Id = dto.UserId,
                DateSuggestion = dto.DateSuggestion,
                OriginalPlace = dto.OriginalPlace,
                SuggestedAlternatives = dto.SuggestedAlternatives,
                Reason = dto.Reason
            };

            var saved = await _suggestionRepository.SaveSuggestionAsync(entity);
            return Ok(saved);
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
                User_Id = 1, // or via ClaimsPrincipal if auth
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
        [HttpPost("translate/french")]
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
        [HttpPut("update")]
        public IActionResult UpdateSuggestion([FromBody] Suggestion suggestion)
        {
            var result = _suggestionRepository.UpdateSuggestion(suggestion);
            return result != null ? Ok(result) : NotFound();
        }
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.