using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface IGptInteractionRepository
    {
        Task SaveInteractionAsync(string prompt, string response, DateTime timestamp);
        Task<IEnumerable<GPTInteraction>> GetAllInteractionsAsync();
        Task<GPTInteraction?> GetByIdAsync(int id);
    }
}


























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.