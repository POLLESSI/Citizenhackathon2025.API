
using Citizenhackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface IGptInteractionRepository
    {
        Task SaveInteractionAsync(string prompt, string response, DateTime timestamp);
        Task<IEnumerable<GPTInteraction>> GetAllInteractionsAsync();
    }
}
