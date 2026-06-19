using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Interfaces.OpenWeather;
using CitizenHackathon2025.Hubs.Services;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class WeatherService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly IMongoSnapshotWriter _mongoSnapshotWriter;
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(IServiceProvider sp, IMongoSnapshotWriter mongoSnapshotWriter, ILogger<WeatherService> logger)
        {
            _sp = sp;
            _mongoSnapshotWriter = mongoSnapshotWriter;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("WeatherService started.");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        using var scope = _sp.CreateScope();
                        var app = scope.ServiceProvider.GetRequiredService<IWeatherForecastAppService>();
                        await app.GenerateAsync(stoppingToken); // generates + broadcast + alert

                        _logger.LogInformation("New forecast broadcasted successfully.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[WeatherService] Critical error while generating/broadcasting forecast.");
                    }

                    try { await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken); }
                    catch (OperationCanceledException) { }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Mongo snapshot write failed. SQL flow continues.");
            }
        }

    }
}































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.