using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.Helpers;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public class TrafficConditionRepository : ITrafficConditionRepository
    {
    #nullable disable
        private readonly System.Data.IDbConnection _connection;
        private readonly ILogger<TrafficConditionRepository> _logger;

        public TrafficConditionRepository(IDbConnection connection, ILogger<TrafficConditionRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public Task<IEnumerable<TrafficCondition>> GetLatestTrafficConditionAsync(int limit = 10, CancellationToken ct = default)
        {
            const string sql = @"
                            SELECT TOP(@Limit)
                                Id, Latitude, Longitude, DateCondition, CongestionLevel, IncidentType,
                                Provider, ExternalId, Fingerprint, LastSeenAt, Title, Road, Severity, GeomWkt, Active
                            FROM dbo.TrafficCondition
                            WHERE Active = 1
                            ORDER BY DateCondition DESC;";
            var cmd = new CommandDefinition(sql, new { Limit = limit }, cancellationToken: ct);
            return _connection.QueryAsync<TrafficCondition>(cmd);
        }
        public async Task<TrafficCondition?> GetByIdAsync(int id)
        {
            try
            {
                const string sql = @"SELECT Id, Latitude, Longitude, DateCondition, CongestionLevel, IncidentType, Active
                             FROM dbo.TrafficCondition
                             WHERE Id = @Id AND Active = 1";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Id", id, DbType.Int64);

                var trafficCondition = await _connection.QueryFirstOrDefaultAsync<TrafficCondition>(sql, parameters);
                return trafficCondition;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Traffic Condition by Id: {ex.ToString}");
                return null;
            }
           
        }
        public async Task<TrafficCondition> SaveTrafficConditionAsync(TrafficCondition trafficCondition)
        {
            try
            {
                const string sql = @"
                            INSERT INTO dbo.TrafficCondition
                             (Latitude, Longitude, DateCondition, CongestionLevel, IncidentType)   -- pas Active
                            OUTPUT INSERTED.Id, INSERTED.Latitude, INSERTED.Longitude,
                                   INSERTED.DateCondition, INSERTED.CongestionLevel,
                                   INSERTED.IncidentType, INSERTED.Active
                            VALUES (@Latitude, @Longitude, @DateCondition, @CongestionLevel, @IncidentType);";

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Latitude", trafficCondition.Latitude);
                parameters.Add("@Longitude", trafficCondition.Longitude);
                parameters.Add("@DateCondition", trafficCondition.DateCondition);
                parameters.Add("@CongestionLevel", trafficCondition.CongestionLevel);
                parameters.Add("@IncidentType", trafficCondition.IncidentType);

                var saved = await _connection.QuerySingleAsync<TrafficCondition>(sql, parameters);
                return saved;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding Traffic Condition: {ex}");
                return null;
            }
        }
        public TrafficCondition UpdateTrafficCondition(TrafficCondition trafficCondition)
        {
            if (trafficCondition == null || trafficCondition.Id <= 0)
            {
                throw new ArgumentException("Invalid traffic condition to update.", nameof(trafficCondition));
            }
            try
            {
                const string sql = @"
                            UPDATE dbo.TrafficCondition
                            SET Latitude        = @Latitude,
                                Longitude       = @Longitude,
                                DateCondition   = @DateCondition,
                                CongestionLevel = @CongestionLevel,
                                IncidentType    = @IncidentType
                            WHERE Id = @Id AND Active = 1;";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Id", trafficCondition.Id);
                parameters.Add("@Latitude", trafficCondition.Latitude);
                parameters.Add("@Longitude", trafficCondition.Longitude);
                parameters.Add("@DateCondition", trafficCondition.DateCondition);
                parameters.Add("@CongestionLevel", trafficCondition.CongestionLevel);
                parameters.Add("@IncidentType", trafficCondition.IncidentType);

                var affectedRows = _connection.Execute(sql, parameters);
                return affectedRows > 0 ? trafficCondition : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating Traffic Condition: {ex}");
            }
            return null;
        }
        public async Task<int> ArchivePastTrafficConditionsAsync()
        {
            const string sql = @"
                            UPDATE [TrafficCondition]
                            SET [Active] = 0
                            WHERE [Active] = 1
                              AND [DateCondition] < DATEADD(DAY, -1, CAST(GETDATE() AS DATETIME2(0)));";

            try
            {
                var affectedRows = await _connection.ExecuteAsync(sql);
                _logger.LogInformation("{Count} Traffic Condition(s) archived.", affectedRows);
                return affectedRows;
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error archiving past Traffic Conditions.");
                return 0;
            }
        }

        public async Task<TrafficCondition?> UpsertTrafficConditionAsync(TrafficCondition tc)
        {
            TrafficUpsertIdentity.Ensure(tc, defaultProvider: "manual");

            if (tc.Fingerprint is null || tc.Fingerprint.Length != 32)
                throw new ArgumentException("Fingerprint must be 32 bytes.", nameof(tc));

            const string sql = @"EXEC dbo.sp_TrafficCondition_Upsert
                            @Latitude, @Longitude, @DateCondition, @CongestionLevel, @IncidentType,
                            @Provider, @ExternalId, @Fingerprint, @LastSeenAt;";

            var p = new DynamicParameters();
            p.Add("@Latitude", tc.Latitude);
            p.Add("@Longitude", tc.Longitude);
            p.Add("@DateCondition", tc.DateCondition);
            p.Add("@CongestionLevel", tc.CongestionLevel);
            p.Add("@IncidentType", tc.IncidentType);

            p.Add("@Provider", tc.Provider);
            p.Add("@ExternalId", tc.ExternalId);
            p.Add("@Fingerprint", tc.Fingerprint, DbType.Binary, size: 32);
            p.Add("@LastSeenAt", tc.LastSeenAt);

            try
            {
                return await _connection.QuerySingleAsync<TrafficCondition>(sql, p);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UPSERT failed provider={Provider} externalId={ExternalId}", tc.Provider, tc.ExternalId);
                return null;
            }
        }
    }
}

































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.