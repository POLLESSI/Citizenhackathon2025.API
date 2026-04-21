using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface IGptInteractionRepository
    {
        Task<GPTInteraction?> UpsertInteractionAsync(GPTInteraction interaction);
        Task<GPTInteraction> CreatePendingAsync(GPTInteraction interaction, CancellationToken ct = default);
        Task SaveInteractionAsync(string prompt, string response, DateTime timestamp);
        Task SaveInteractionAsync(GPTInteraction interaction);
        Task<IEnumerable<GPTInteraction>> GetAllInteractionsAsync();
        Task<GPTInteraction?> GetByIdAsync(int id);
        Task<bool> DeactivateInteractionAsync(int id);
        Task<int> ArchivePastGptInteractionsAsync();
        Task<bool> UpdateResponseAsync(int interactionId, string response, CancellationToken ct = default);
        Task<bool> MarkFailedAsync(int interactionId, string? errorMessage, CancellationToken ct = default);
    }
}


























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.