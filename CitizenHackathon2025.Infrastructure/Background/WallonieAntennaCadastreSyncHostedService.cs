using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Wallonie.Antennas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class WallonieAntennaCadastreSyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WallonieAntennaCadastreSyncHostedService> _logger;

    public WallonieAntennaCadastreSyncHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<WallonieAntennaCadastreSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SyncOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SyncOnceAsync(stoppingToken);
        }
    }

    private async Task SyncOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var client = scope.ServiceProvider.GetRequiredService<WallonieAntennaCadastreClient>();
            var repo = scope.ServiceProvider.GetRequiredService<ICrowdInfoAntennaRepository>();

            var sites = await client.GetSitesAsync(ct);

            var count = 0;

            foreach (var site in sites)
            {
                if (string.IsNullOrWhiteSpace(site.ExternalId))
                    continue;

                var name = BuildName(site);

                var description =
                    $"Wallonie antenna cadastre | Commune={site.Commune} | Rue={site.Rue} | Type={site.TypeImplantation} | Operators={site.OperatorCount}";

                var antenna = new CrowdInfoAntenna
                {
                    ExternalSource = "WALLONIE_ANTENNES",
                    ExternalId = site.ExternalId,
                    Name = name,
                    Latitude = site.Latitude,
                    Longitude = site.Longitude,
                    Description = description.Length > 256 ? description[..256] : description,
                    MaxCapacity = EstimateCapacity(site.OperatorCount),
                    Active = true
                };

                await repo.UpsertFromCadastreAsync(antenna, ct);
                count++;
            }

            _logger.LogInformation("Wallonie antenna cadastre sync completed. Upserted={Count}", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wallonie antenna cadastre sync failed.");
        }
    }

    private static string BuildName(WallonieAntennaSiteDto site)
    {
        var raw = $"Antenne {site.Commune ?? site.Localite ?? "Wallonie"} #{site.ExternalId}";
        return raw.Length <= 64 ? raw : raw[..64];
    }

    private static int? EstimateCapacity(int? operatorCount)
    {
        if (operatorCount is null or <= 0)
            return 100;

        return Math.Clamp(operatorCount.Value * 100, 100, 1000);
    }
}



















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.