using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class GptInteractionService
    {
        private readonly IGPTRepository _gptRepository;
        public GptInteractionService(IGPTRepository gptRepository) => _gptRepository = gptRepository;

        public Task SavePromptAsync(string prompt, string response)
        {
            // Let the DB set CreatedAt (SYSUTCDATETIME), the repo does not use it.
            var entity = new GPTInteraction
            {
                Prompt = prompt,
                Response = response,
                // CreatedAt ignored: DB consistency
                Active = true
            };
            return _gptRepository.SaveInteractionAsync(entity);
        }
    }
}

















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.