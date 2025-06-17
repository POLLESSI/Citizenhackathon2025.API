using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Citizenhackathon2025.Application.Interfaces;
using CitizenHackathon2025.Hubs.Services;
using Polly;
using Polly.Wrap;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Citizenhackathon2025.Application.Services
{
    public class WeatherService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WeatherService> _logger;
        private readonly AsyncPolicyWrap _resiliencePolicy;
        private Timer _timer;
        private readonly string[] _summaries = new[] {"Sunny", "Cloudy", "Rainy", "Stormy", "Snowy", "Foggy"};
        private readonly Random _rng = new();

        public WeatherService(IServiceProvider serviceProvider, ILogger<WeatherService> logger, AsyncPolicyWrap resiliencePolicy)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            var retryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (ex, delay, retryCount, ctx) =>
                {
                    logger.LogWarning(ex, $"[WeatherService] Retry {retryCount} after {delay.TotalSeconds}s");
                });

            var circuitBreakerPolicy = Policy.Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromMinutes(1),
                    onBreak: (ex, breakDelay) =>
                    {
                        logger.LogWarning($"[WeatherService] Circuit open for {breakDelay.TotalSeconds}s due to {ex.Message}");
                    },
                    onReset: () =>
                    {
                        logger.LogInformation("[WeatherService] Circuit closed. Normal operations resumed.");
                    });

            _resiliencePolicy = resiliencePolicy;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WeatherService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _resiliencePolicy.ExecuteAsync(async () =>
                    {
                        using var scope = _serviceProvider.CreateScope();

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

                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);

            }
            _logger.LogInformation("WeatherService stopped.");
        }
        public async Task<Task> StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
            return Task.CompletedTask;
        }
        private async void DoWork(object state)
        {
            using var scope = _serviceProvider.CreateScope();
            var forecastService = scope.ServiceProvider.GetRequiredService<IWeatherForecastService>();

            try
            {
                await forecastService.SendWeatherToAllClientsAsync();
            }
            catch (Exception ex)
            {

                Console.WriteLine($"[WeatherService] Error: {ex.Message}");
            }
            
            // TODO: exploiter les données météo ici
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}






























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.