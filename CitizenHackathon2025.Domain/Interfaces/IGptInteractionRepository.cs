using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface IGptInteractionRepository
    {
        Task<GPTInteraction?> UpsertInteractionAsync(GPTInteraction interaction);
        Task SaveInteractionAsync(string prompt, string response, DateTime timestamp);
        Task SaveInteractionAsync(GPTInteraction interaction);
        Task<IEnumerable<GPTInteraction>> GetAllInteractionsAsync();
        Task<GPTInteraction?> GetByIdAsync(int id);
        //Task<string> AskAsync(string question);
        Task<bool> DeactivateInteractionAsync(int id);
        Task<int> ArchivePastGptInteractionsAsync();
    }
}



























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.