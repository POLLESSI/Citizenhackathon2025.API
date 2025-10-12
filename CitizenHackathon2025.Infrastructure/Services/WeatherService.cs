using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Hubs.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Wrap;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class WeatherService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<WeatherService> _logger;
        private readonly AsyncPolicyWrap _policy;   

        public WeatherService(IServiceProvider serviceProvider,
                              ILogger<WeatherService> logger,
                              AsyncPolicyWrap resiliencePolicy)
        {
            _sp = serviceProvider;
            _logger = logger;
            _policy = resiliencePolicy ?? throw new ArgumentNullException(nameof(resiliencePolicy));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WeatherService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _policy.ExecuteAsync(async () =>
                    {
                        using var scope = _sp.CreateScope();
                        var forecastService = scope.ServiceProvider.GetRequiredService<IWeatherForecastService>();
                        var hubService = scope.ServiceProvider.GetRequiredService<IWeatherHubService>();

                        var forecast = await forecastService.GenerateNewForecastAsync();
                        await hubService.BroadcastWeatherAsync(forecast, stoppingToken);

                        _logger.LogInformation("New forecast broadcasted successfully.");
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[WeatherService] A critical error occurred even after retries.");
                }

                // cadence
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                }
                catch (OperationCanceledException) { /* shutdown */ }
            }

            _logger.LogInformation("WeatherService stopped.");
        }
    }
}






























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.