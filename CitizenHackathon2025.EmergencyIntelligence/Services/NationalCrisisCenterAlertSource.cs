using CitizenHackathon2025.EmergencyIntelligence.Interfaces;
using CitizenHackathon2025.EmergencyIntelligence.Records;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.EmergencyIntelligence.Services
{
    public sealed class NationalCrisisCenterAlertSource : INationalCrisisCenterAlertSource
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NationalCrisisCenterAlertSource> _logger;
        private readonly TimeProvider _timeProvider;

        public NationalCrisisCenterAlertSource(HttpClient httpClient, ILogger<NationalCrisisCenterAlertSource> logger, TimeProvider timeProvider)
        {
            _httpClient = httpClient;
            _logger = logger;
            _timeProvider = timeProvider;
        }

        public string SourceCode => "BE-NCCN";

        public Task<EmergencyAlertBatch> FetchAsync(EmergencyAlertCursor cursor, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("National Crisis Center source {SourceCode} called; " + "remote provider is not configured yet.",SourceCode);

            var batch = new EmergencyAlertBatch(
                Alerts: Array.Empty<RawEmergencyAlert>(),
                ETag: cursor.ETag,
                LastModifiedUtc: cursor.LastModifiedUtc,
                ContinuationToken: cursor.ContinuationToken,
                FetchedAtUtc: _timeProvider.GetUtcNow());

            return Task.FromResult(batch);
        }
    }
}




















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.