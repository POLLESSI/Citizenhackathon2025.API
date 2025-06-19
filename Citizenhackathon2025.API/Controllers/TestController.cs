using Citizenhackathon2025.Application.UseCases;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CitizenHackathon2025.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly CitizenSuggestionService _service;

        public TestController(CitizenSuggestionService service)
        {
            _service = service;
        }

        [HttpGet("suggestion")]
        public async Task<IActionResult> GetSuggestion()
        {
            var result = await _service.GetPersonalizedSuggestionsAsync("Paris", 1);
            return Ok(result);
        }
    }
}
