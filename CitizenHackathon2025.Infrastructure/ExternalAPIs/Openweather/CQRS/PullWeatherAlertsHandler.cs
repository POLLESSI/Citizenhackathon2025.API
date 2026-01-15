using CitizenHackathon2025.Application.Interfaces;
using MediatR;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.CQRS
{
    public sealed class PullWeatherAlertsHandler : IRequestHandler<PullWeatherAlertsCommand, (int Alerts, int Forecasts)>
    {
        private readonly IWeatherAlertsIngestionService _svc;
        public PullWeatherAlertsHandler(IWeatherAlertsIngestionService svc) => _svc = svc;

        public async Task<(int Alerts, int Forecasts)> Handle(PullWeatherAlertsCommand request, CancellationToken ct)
        {
            var (a, f) = await _svc.PullAndStoreAsync(request.Latitude, request.Longitude, ct);
            return (a, f);
        }
    }
}


































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.