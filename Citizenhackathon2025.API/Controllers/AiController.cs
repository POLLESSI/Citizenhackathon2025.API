using CitizenHackathon2025.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace CitizenHackathon2025.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiController : ControllerBase
    {
        private readonly ChatGptService _chatGpt;

        public AiController(ChatGptService chatGpt)
        {
            _chatGpt = chatGpt;
        }
        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] string question)
        {
            if (string.IsNullOrEmpty(question))
                return BadRequest("Question cannot be empty");
            var response = await _chatGpt.AskChatGptAsync(question);
            return Ok(response);
        }
    }
}





























// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.