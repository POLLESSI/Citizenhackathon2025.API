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

        public async Task<IEnumerable<TrafficCondition?>> GetLatestTrafficConditionAsync(CancellationToken ct)
        {
            try
            {
                const string sql = @"
                        SELECT TOP 10 *
                        FROM TrafficCondition
                        WHERE Active = 1
                        ORDER BY DateCondition DESC";

                var cmd = new CommandDefinition(sql, cancellationToken: ct);
                var list = await _connection.QueryAsync<TrafficCondition>(cmd);
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving Traffic Condition: {ex.Message}");
                return Array.Empty<TrafficCondition>();
            }
        }
        public async Task<TrafficCondition?> GetByIdAsync(int id)
        {
            try
            {
                const string sql = "SELECT Id, Latitude, Longitude, DateCondition, CongestionLevel, IncidentType FROM TrafficConditions WHERE Id = @Id AND Active = 1";
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
                INSERT INTO TrafficCondition
                (Latitude, Longitude, DateCondition, CongestionLevel, IncidentType, Active)
                VALUES
                (@Latitude, @Longitude, @DateCondition, @CongestionLevel, @IncidentType, 1);
                SELECT CAST(SCOPE_IDENTITY() AS int)";

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Latitude", trafficCondition.Latitude);
                parameters.Add("@Longitude", trafficCondition.Longitude);
                parameters.Add("@DateCondition", trafficCondition.DateCondition);
                parameters.Add("@CongestionLevel", trafficCondition.CongestionLevel);
                parameters.Add("@IncidentType", trafficCondition.IncidentType);

                var newId = await _connection.ExecuteScalarAsync<int>(sql, parameters);
  
                return trafficCondition;
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
                UPDATE TrafficCondition
                SET Latitude = @Latitude,
                    Longitude = @Longitude,
                    DateCondition = @DateCondition,
                    CongestionLevel = @CongestionLevel,
                    IncidentType = @IncidentType
                WHERE Id = @Id";
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