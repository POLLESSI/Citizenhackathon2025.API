using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Shared.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.Infrastructure.Background
{
    public sealed class ExpiredDataArchiverHostedService : SafePeriodicBackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly PipelineOptions _options;

        protected override string ServiceName => nameof(ExpiredDataArchiverHostedService);

        protected override TimeSpan Period =>
            TimeSpan.FromSeconds(Math.Max(30, _options.Archiver.PeriodSeconds));

        public ExpiredDataArchiverHostedService(
            IServiceScopeFactory scopeFactory,
            IOptions<PipelineOptions> options,
            ILogger<ExpiredDataArchiverHostedService> logger)
            : base(logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
        }

        protected override async Task ExecuteIterationAsync(CancellationToken ct)
        {
            if (!_options.Archiver.Enabled)
                return;

            using var scope = _scopeFactory.CreateScope();

            var crowdRepo = scope.ServiceProvider.GetRequiredService<ICrowdInfoRepository>();
            var trafficRepo = scope.ServiceProvider.GetRequiredService<ITrafficConditionRepository>();
            var weatherApp = scope.ServiceProvider.GetRequiredService<IWeatherForecastAppService>();
            var antennaConnectionRepo = scope.ServiceProvider.GetRequiredService<ICrowdInfoAntennaConnectionRepository>();

            await crowdRepo.ArchivePastCrowdInfosAsync(ct);
            await trafficRepo.ArchivePastTrafficConditionsAsync(ct);
            await weatherApp.ArchiveExpiredAsync(ct);

            await antennaConnectionRepo.ArchiveAndDeleteExpiredAsync(
                timeoutSeconds: 60,
                batchSize: 10_000,
                ct);
        }
    }
}







































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.