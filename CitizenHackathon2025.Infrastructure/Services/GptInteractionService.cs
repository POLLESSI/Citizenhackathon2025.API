using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class GptInteractionService
    {
        private readonly IGPTRepository _gptRepository;
        public GptInteractionService(IGPTRepository gptRepository)
        {
            _gptRepository = gptRepository;
        }
        public async void SavePrompt(string prompt, string response)
        {
            await _gptRepository.SaveInteractionAsync(new GPTInteraction
            {
                Prompt = prompt,
                Response = response,
                CreatedAt = DateTime.UtcNow
            });
            // ... Run command here with SqlConnection
        }
    }
}

















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.