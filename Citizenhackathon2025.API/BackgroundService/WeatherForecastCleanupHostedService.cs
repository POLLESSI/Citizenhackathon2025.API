using CitizenHackathon2025.Domain.Interfaces;

namespace CitizenHackathon2025.API.BackgroundServices;

public sealed class WeatherForecastCleanupHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WeatherForecastCleanupHostedService> _logger;

    public WeatherForecastCleanupHostedService(
        IServiceProvider serviceProvider,
        ILogger<WeatherForecastCleanupHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var repo = scope.ServiceProvider
                    .GetRequiredService<IWeatherForecastRepository>();

                var archived = await repo.ArchivePastWeatherForecastsAsync(stoppingToken);

                if (archived > 0)
                {
                    _logger.LogInformation(
                        "Strict weather cleanup archived {Count} expired forecast(s).",
                        archived);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Strict weather cleanup failed.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }
}

















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.