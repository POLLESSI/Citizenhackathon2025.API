using Azure.Core;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Collections;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Repositories;
using CitizenHackathon2025.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class TrafficConditionService : ITrafficConditionService
    {
    #nullable disable
        private readonly ITrafficConditionRepository _trafficConditionRepository;
        private readonly ITrafficSnapshotRepository _trafficSnapshotRepository;
        private readonly ILogger<TrafficConditionService> _logger;

        public TrafficConditionService(ITrafficConditionRepository trafficConditionRepository, ITrafficSnapshotRepository trafficSnapshotRepository, ILogger<TrafficConditionService> logger)
        {
            _trafficConditionRepository = trafficConditionRepository;
            _trafficSnapshotRepository = trafficSnapshotRepository;
            _logger = logger;
        }

        public Task<TrafficDTO> CheckRoadAsync(Domain.Entities.ValueObjects.Location from, string to)
        {
            throw new NotImplementedException();
        }

        public Task CheckRoadAsync(Domain.ValueObjects.Location userPosition, string destination)
        {
            throw new NotImplementedException();
        }

        public Task<TrafficAnalysisResult> CheckRoadAsync(string from, string to)
        {
            throw new NotImplementedException();
        }

        public async Task<TrafficCondition?> GetByIdAsync(int id)
        {
            return await _trafficConditionRepository.GetByIdAsync(id);
        }

        public async Task<TrafficCondition> SaveTrafficConditionAsync(TrafficCondition trafficCondition)
        {
            var saved = await _trafficConditionRepository.SaveTrafficConditionAsync(trafficCondition);

            try
            {
                var snapshot = new TrafficSnapshotDocument
                {
                    TrafficConditionId = saved.Id,
                    Source = saved.Provider,
                    RoadName = saved.Road,
                    FromLocation = null,
                    ToLocation = null,
                    Severity = saved.Severity?.ToString(),
                    Status = saved.CongestionLevel,
                    Description = saved.Title ?? saved.IncidentType,
                    Latitude = (double)saved.Latitude,
                    Longitude = (double)saved.Longitude,
                    ObservedAtUtc = saved.LastSeenAt,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _trafficSnapshotRepository.InsertAsync(snapshot);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Mongo traffic snapshot failed. SQL traffic condition remains saved.");
            }

            return saved;
        }
        public TrafficCondition UpdateTrafficCondition(TrafficCondition trafficCondition)
        {
            try
            {
                var updatedTrafficCondition = _trafficConditionRepository.UpdateTrafficCondition(trafficCondition);
                if (updatedTrafficCondition == null)
                {
                    throw new KeyNotFoundException("Traffic condition not found for update.");
                }
                return updatedTrafficCondition;
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex)
            {
                Console.WriteLine($"Validation error : {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating traffic condition : {ex}");
            }
            return null;
        }

        public async Task<IEnumerable<TrafficCondition>> GetLatestTrafficConditionAsync(int limit = 10, CancellationToken cancellationToken = default)
        {// ❌ Before : _trafficConditionRepository.GetLatestTrafficConditionAsync();
            var trafficConditions = await _trafficConditionRepository.GetLatestTrafficConditionAsync(limit, cancellationToken); // ✅
            return trafficConditions;
        }

        public async Task<int> ArchivePastTrafficConditionsAsync()
        {
            string sql = "UPDATE TrafficCondition SET Active = 0 WHERE DateCondition < @Threshold AND Active = 1";
            var parameters = new { Threshold = DateTime.UtcNow.Date.AddDays(-2) };
            return await _trafficConditionRepository.ArchivePastTrafficConditionsAsync();
        }

        public async Task<TrafficCondition> UpsertTrafficConditionAsync(TrafficCondition trafficCondition)
            => await _trafficConditionRepository.UpsertTrafficConditionAsync(trafficCondition);
    }
}





































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.