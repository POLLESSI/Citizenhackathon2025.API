using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Shared.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.Infrastructure.Background
{
    public sealed class WeatherForecastCollectorHostedService : SafePeriodicBackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<WeatherForecastHub> _hub;
        private readonly PipelineOptions _options;

        protected override string ServiceName => nameof(WeatherForecastCollectorHostedService);

        protected override TimeSpan Period =>
            TimeSpan.FromSeconds(Math.Max(30, _options.WeatherForecast.PeriodSeconds));

        public WeatherForecastCollectorHostedService(
            IServiceScopeFactory scopeFactory,
            IHubContext<WeatherForecastHub> hub,
            IOptions<PipelineOptions> options,
            ILogger<WeatherForecastCollectorHostedService> logger)
            : base(logger)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
            _options = options.Value;
        }

        protected override async Task ExecuteIterationAsync(CancellationToken ct)
        {
            if (!_options.WeatherForecast.Enabled)
                return;

            using var scope = _scopeFactory.CreateScope();

            var app = scope.ServiceProvider.GetRequiredService<IWeatherForecastAppService>();

            var lat = _options.WeatherForecast.DefaultLatitude;
            var lon = _options.WeatherForecast.DefaultLongitude;

            var (alertsUpserted, forecastSaved) = await app.PullAsync(lat, lon, ct);

            if (forecastSaved is not null)
            {
                await _hub.Clients.All.SendAsync(
                    "WeatherRefreshRequested",
                    "openweather-sync",
                    ct);
            }
        }
    }
}















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.