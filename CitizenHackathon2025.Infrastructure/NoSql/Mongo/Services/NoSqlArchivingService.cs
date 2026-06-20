using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Collections;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.NoSql.Mongo.Services
{
    public interface INoSqlArchivingService
    {
        Task ArchiveGptInteractionAsync(
            GptInteractionDocument document,
            CancellationToken ct = default);

        Task ArchiveWeatherSnapshotAsync(
            WeatherSnapshotDocument document,
            CancellationToken ct = default);

        Task ArchiveTrafficSnapshotAsync(
            TrafficSnapshotDocument document,
            CancellationToken ct = default);

        Task ArchiveCrowdSnapshotAsync(
            CrowdSnapshotDocument document,
            CancellationToken ct = default);

        Task ArchiveSignalRDiagnosticAsync(
            SignalRDiagnosticDocument document,
            CancellationToken ct = default);
    }

    public sealed class NoSqlArchivingService : INoSqlArchivingService
    {
        private readonly IMongoSnapshotWriter _snapshotWriter;
        private readonly ISignalRDiagnosticRepository _signalRDiagnosticRepository;
        private readonly ILogger<NoSqlArchivingService> _logger;

        public NoSqlArchivingService(
            IMongoSnapshotWriter snapshotWriter,
            ISignalRDiagnosticRepository signalRDiagnosticRepository,
            ILogger<NoSqlArchivingService> logger)
        {
            _snapshotWriter = snapshotWriter;
            _signalRDiagnosticRepository = signalRDiagnosticRepository;
            _logger = logger;
        }

        public async Task ArchiveGptInteractionAsync(
            GptInteractionDocument document,
            CancellationToken ct = default)
        {
            try
            {
                await _snapshotWriter.WriteGptInteractionAsync(document, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "NoSQL GPT archive failed.");
            }
        }

        public async Task ArchiveWeatherSnapshotAsync(
            WeatherSnapshotDocument document,
            CancellationToken ct = default)
        {
            try
            {
                await _snapshotWriter.WriteWeatherSnapshotAsync(document, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "NoSQL weather archive failed.");
            }
        }

        public async Task ArchiveTrafficSnapshotAsync(
            TrafficSnapshotDocument document,
            CancellationToken ct = default)
        {
            try
            {
                await _snapshotWriter.WriteTrafficSnapshotAsync(document, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "NoSQL traffic archive failed.");
            }
        }

        public async Task ArchiveCrowdSnapshotAsync(
            CrowdSnapshotDocument document,
            CancellationToken ct = default)
        {
            try
            {
                await _snapshotWriter.WriteCrowdSnapshotAsync(document, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "NoSQL crowd archive failed.");
            }
        }

        public async Task ArchiveSignalRDiagnosticAsync(
            SignalRDiagnosticDocument document,
            CancellationToken ct = default)
        {
            try
            {
                await _signalRDiagnosticRepository.InsertAsync(document, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "NoSQL SignalR diagnostic archive failed.");
            }
        }
    }
}



























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.