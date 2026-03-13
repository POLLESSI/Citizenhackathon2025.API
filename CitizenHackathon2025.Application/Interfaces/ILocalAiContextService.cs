using CitizenHackathon2025.Domain.DTOs;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface ILocalAiContextService
    {
        Task<LocalAiContextDTO> BuildContextAsync(
            string prompt,
            double? latitude,
            double? longitude,
            CancellationToken ct = default);

        string BuildPrompt(LocalAiContextDTO context);
    }
}
