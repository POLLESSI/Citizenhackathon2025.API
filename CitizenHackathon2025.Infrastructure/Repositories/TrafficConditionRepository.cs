using Dapper;
using CitizenHackathon2025.Domain.Interfaces;
using System.Data;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public class TrafficConditionRepository : ITrafficConditionRepository
    {
    #nullable disable
        private readonly System.Data.IDbConnection _connection;

        public TrafficConditionRepository(System.Data.IDbConnection connection)
        {
            _connection = connection;
        }

        public Task<IEnumerable<TrafficCondition>> GetLatestTrafficConditionAsync(int limit = 10, CancellationToken ct = default)
        {
            const string sql = @"
                            SELECT TOP(@Limit)
                                Id, Latitude, Longitude, DateCondition, CongestionLevel, IncidentType, Active
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
    }
}

































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.