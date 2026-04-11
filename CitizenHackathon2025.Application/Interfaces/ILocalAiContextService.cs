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























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.