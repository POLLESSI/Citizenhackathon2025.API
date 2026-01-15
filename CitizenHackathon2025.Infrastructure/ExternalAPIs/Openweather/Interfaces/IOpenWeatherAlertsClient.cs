using CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Models;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Interfaces
{
    public interface IOpenWeatherAlertsClient
    {
        Task<OneCallResponse> GetOneCallAsync(decimal lat, decimal lon, CancellationToken ct = default);
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.