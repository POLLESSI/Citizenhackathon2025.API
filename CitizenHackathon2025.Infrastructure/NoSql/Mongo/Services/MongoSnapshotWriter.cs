using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Collections;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Repositories;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.NoSql.Mongo.Services
{
    public sealed class MongoSnapshotWriter : IMongoSnapshotWriter
    {
        private readonly IWeatherSnapshotRepository _weatherRepository;
        private readonly ICrowdSnapshotRepository _crowdRepository;
        private readonly IGptInteractionNoSqlRepository _gptRepository;
        private readonly ITrafficSnapshotRepository _trafficRepository;
        private readonly ILogger<MongoSnapshotWriter> _logger;

        public MongoSnapshotWriter(IWeatherSnapshotRepository weatherRepository, ICrowdSnapshotRepository crowdRepository, IGptInteractionNoSqlRepository gptRepository, ITrafficSnapshotRepository trafficRepository, ILogger<MongoSnapshotWriter> logger)
        {
            _weatherRepository = weatherRepository;
            _crowdRepository = crowdRepository;
            _gptRepository = gptRepository;
            _trafficRepository = trafficRepository;
            _logger = logger;
        }

        public async Task WriteWeatherSnapshotAsync(WeatherSnapshotDocument snapshot, CancellationToken ct = default)
        {
            try
            {
                await _weatherRepository.InsertAsync(snapshot, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Weather Mongo snapshot write failed.");
            }
        }

        public async Task WriteCrowdSnapshotAsync(CrowdSnapshotDocument snapshot, CancellationToken ct = default)
        {
            try
            {
                await _crowdRepository.InsertAsync(snapshot, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Crowd Mongo snapshot write failed.");
            }
        }

        public async Task WriteGptInteractionAsync(GptInteractionDocument document, CancellationToken ct = default)
        {
            try
            {
                await _gptRepository.InsertAsync(document, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GPT Mongo interaction write failed.");
            }
        }

        public async Task WriteTrafficSnapshotAsync(TrafficSnapshotDocument snapshot, CancellationToken ct = default)
        {
            try
            {
                await _trafficRepository.InsertAsync(snapshot, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Traffic Mongo snapshot write failed.");
            }
        }
    }
}

























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.