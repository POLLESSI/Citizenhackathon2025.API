using CitizenHackathon2025.EmergencyIntelligence.Interfaces;
using CitizenHackathon2025.EmergencyIntelligence.Models;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.EmergencyIntelligence.Services
{
    public sealed class EmergencyAlertSyncOrchestrator : IEmergencyAlertSyncOrchestrator
    {
        private readonly IEnumerable<IEmergencyAlertSource> _sources;
        private readonly IReadOnlyDictionary<string, IEmergencyAlertNormalizer> _normalizers;
        private readonly IEmergencyAlertRepository _repository;
        private readonly IEmergencyAlertPublisher _publisher;
        private readonly ILogger<EmergencyAlertSyncOrchestrator> _logger;
        public EmergencyAlertSyncOrchestrator(IEnumerable<IEmergencyAlertSource> sources, IEnumerable<IEmergencyAlertNormalizer> normalizers, IEmergencyAlertRepository repository, IEmergencyAlertPublisher publisher, ILogger<EmergencyAlertSyncOrchestrator> logger)
        {
            _sources = sources ?? throw new ArgumentNullException(nameof(sources));

            _repository = repository ?? throw new ArgumentNullException(nameof(repository));

            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _normalizers = normalizers
                .GroupBy(x => x.SourceCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.Single(), StringComparer.OrdinalIgnoreCase);
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

                    _logger.LogInformation("Emergency source {SourceCode} " + "returned {AlertCount} raw alerts.", source.SourceCode, batch.Alerts.Count);

                    if (!_normalizers.TryGetValue(source.SourceCode, out var normalizer))
                    {
                        if (batch.Alerts.Count > 0)
                        {
                            _logger.LogWarning("No emergency normalizer " + "registered for source " + "{SourceCode}.", source.SourceCode);
                        }

                        continue;
                    }

                    var normalizedAlerts = new List<EmergencyAlert>(batch.Alerts.Count);

                    foreach (var raw in batch.Alerts)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            var normalized = normalizer.Normalize(raw);

                            normalizedAlerts.Add(normalized);

                            _logger.LogInformation(
                                "Emergency alert normalized. " +
                                "Source={SourceCode}, " +
                                "ExternalId={ExternalId}, " +
                                "Severity={Severity}, " +
                                "Urgency={Urgency}, " +
                                "Status={Status}, " +
                                "Official={Official}.",
                                normalized.SourceCode,
                                normalized.ExternalId,
                                normalized.Severity,
                                normalized.Urgency,
                                normalized.Status,
                                normalized.IsOfficial);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Emergency alert normalization " +
                                "failed. Source={SourceCode}, " +
                                "ExternalId={ExternalId}.",
                                raw.SourceCode,
                                raw.ExternalId);
                        }
                    }

                    _logger.LogInformation(
                        "Emergency source {SourceCode} " +
                        "produced {NormalizedCount} " +
                        "normalized alerts.",
                        source.SourceCode,
                        normalizedAlerts.Count);


                    // =================================================
                    // UPSERT / UPDATE / CANCEL
                    // =================================================

                    foreach (var normalized in normalizedAlerts)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var result = await _repository.ApplyAsync(normalized, cancellationToken);

                        /*
                         * First remove previous versions that
                         * have been cancelled or superseded.
                         */
                        foreach (var removed in result.RemovedAlerts)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (removed.Reason == EmergencyAlertRemovalReason.Expired)
                            {
                                await _publisher.PublishExpiredAsync(removed.Alert, cancellationToken);
                            }
                            else
                            {
                                /*
                                 * Both CAP Update and CAP Cancel
                                 * remove the old active version
                                 * from the current UI state.
                                 */
                                await _publisher.PublishCancelledAsync(removed.Alert, cancellationToken);
                            }
                        }


                        /*
                         * Same SourceCode + ExternalId +
                         * PayloadHash:
                         *
                         * no redundant SignalR broadcast.
                         */
                        if (!result.Changed)
                        {
                            continue;
                        }


                        /*
                         * Only active current alerts are
                         * broadcast as UPSERT.
                         */
                        if (result.IsActive)
                        {
                            await _publisher.PublishUpsertedAsync(result.StoredAlert, cancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError( ex, "Emergency source {SourceCode} " + "synchronization failed.", source.SourceCode);
                }
            }

            // =====================================================
            // GLOBAL EXPIRATION
            //
            // Important:
            // once per synchronization cycle,
            // AFTER all sources.
            // =====================================================

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var expiredAlerts = await _repository.ExpireDueAsync(DateTimeOffset.UtcNow, cancellationToken);

                foreach (var expired in expiredAlerts)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await _publisher.PublishExpiredAsync(expired, cancellationToken);
                }


                if (expiredAlerts.Count > 0)
                {
                    _logger.LogInformation("{ExpiredCount} emergency alert(s) " + "expired during synchronization.", expiredAlerts.Count);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Emergency alert expiration phase failed.");
            }
        }
    }
}





















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.