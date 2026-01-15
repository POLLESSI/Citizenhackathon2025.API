namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IWeatherAlertsIngestionService
    {
        Task<(int AlertsUpserted, int ForecastSaved)> PullAndStoreAsync(decimal lat, decimal lon, CancellationToken ct = default);
    }
}














































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.