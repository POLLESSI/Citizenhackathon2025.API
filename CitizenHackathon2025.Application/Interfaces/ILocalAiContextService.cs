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

    public sealed class LocalAiIntent
    {
        public bool NeedEvents { get; init; }
        public bool NeedCrowdCalendar { get; init; }
        public bool NeedCrowdInfo { get; init; }
        public bool NeedTraffic { get; init; }
        public bool NeedWeather { get; init; }
    }

    public sealed class LocalAiContextLimits
    {
        public int MaxEvents { get; init; } = 3;
        public int MaxCrowdCalendar { get; init; } = 3;
        public int MaxCrowdInfo { get; init; } = 3;
        public int MaxTraffic { get; init; } = 2;
        public int MaxWeather { get; init; } = 2;
        public double RadiusKm { get; init; } = 25.0;
    }
}























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.