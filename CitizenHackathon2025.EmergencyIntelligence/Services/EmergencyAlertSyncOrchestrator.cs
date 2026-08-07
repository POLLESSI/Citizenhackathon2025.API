using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.EmergencyIntelligence.Services
{
    using Interfaces;

    public sealed class EmergencyAlertSyncOrchestrator : IEmergencyAlertSyncOrchestrator
    {
        private readonly IEnumerable<IEmergencyAlertSource> _sources;
        private readonly ILogger<EmergencyAlertSyncOrchestrator> _logger;

        public EmergencyAlertSyncOrchestrator(IEnumerable<IEmergencyAlertSource> sources, ILogger<EmergencyAlertSyncOrchestrator> logger)
        {
            _sources = sources;
            _logger = logger;
        }

        public async Task SynchronizeAllAsync(CancellationToken cancellationToken = default)
        {
            foreach (var source in _sources)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var cursor = Records.EmergencyAlertCursor.Empty;

                    var batch = await source.FetchAsync(cursor, cancellationToken);

                    _logger.LogInformation("Emergency source {SourceCode} returned {AlertCount} alerts.", source.SourceCode, batch.Alerts.Count);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Emergency source {SourceCode} synchronization failed.", source.SourceCode);
                }
            }
        }
    }
}





















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.