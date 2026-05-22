using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public class CrowdInfoRepository : ICrowdInfoRepository
    {
    #nullable disable
        private readonly System.Data.IDbConnection _connection;
        private readonly ILogger<CrowdInfoRepository> _logger;

        public CrowdInfoRepository(IDbConnection connection, ILogger<CrowdInfoRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<bool> DeleteCrowdInfoAsync(int id)
        {
            var sql = "DELETE FROM CrowdInfo WHERE Id = @Id";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            var affectedRows = await _connection.ExecuteAsync(sql, parameters);
            return affectedRows > 0;
        }

        public async Task<IEnumerable<CrowdInfo>> GetNearbyCrowdInfoAsync(double? latitude, double? longitude, int radiusKm, CancellationToken ct = default)
        {
            if (!latitude.HasValue || !longitude.HasValue)
                return Enumerable.Empty<CrowdInfo>();

            var sql = @"
                    SELECT * FROM CrowdInfo
                    WHERE Active = 1
                    AND (6371 * ACOS(
                        COS(RADIANS(@Lat)) * COS(RADIANS(Latitude)) *
                        COS(RADIANS(Longitude) - RADIANS(@Lon)) +
                        SIN(RADIANS(@Lat)) * SIN(RADIANS(Latitude))
                    )) <= @RadiusKm
                    ORDER BY Timestamp DESC";

            return await _connection.QueryAsync<CrowdInfo>(sql, new
            {
                Lat = latitude.Value,
                Lon = longitude.Value,
                RadiusKm = radiusKm
            });
        }

        public Task<IEnumerable<CrowdInfo>> GetAllCrowdInfoAsync(int limit = 200, CancellationToken ct = default)
        {
            const string sql = @"
                            SELECT TOP(@Limit)
                                Id,
                                LocationName,
                                Latitude,
                                Longitude,
                                CrowdLevel,
                                [Timestamp],
                                Active
                            FROM dbo.CrowdInfo
                            WHERE Active = 1
                            ORDER BY [Timestamp] DESC;";

            return _connection.QueryAsync<CrowdInfo>(
                new CommandDefinition(sql, new { Limit = limit }, cancellationToken: ct));
        }

        public async Task<CrowdInfo?> GetCrowdInfoByIdAsync(int id)
        {
            var sql = "SELECT * FROM CrowdInfo WHERE Id = @Id";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            var result = await _connection.QuerySingleOrDefaultAsync<CrowdInfo>(sql, parameters);
            return result;
        }

        public async Task<CrowdInfo?> SaveCrowdInfoAsync(CrowdInfo crowdInfo)
        {
            const string sql = @"
                INSERT INTO CrowdInfo (LocationName, Latitude, Longitude, CrowdLevel, Timestamp)
                VALUES (@LocationName, @Latitude, @Longitude, @CrowdLevel, @Timestamp);
                SELECT CAST(SCOPE_IDENTITY() as int);
            ";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@LocationName", crowdInfo.LocationName);
            parameters.Add("@Latitude", crowdInfo.Latitude);
            parameters.Add("@Longitude", crowdInfo.Longitude);
            parameters.Add("@CrowdLevel", crowdInfo.CrowdLevel);
            parameters.Add("@Latitude", crowdInfo.Latitude);     // decimal
            parameters.Add("@Longitude", crowdInfo.Longitude);   // decimal
            parameters.Add("@CrowdLevel", crowdInfo.CrowdLevel); // int
            parameters.Add("@Timestamp", crowdInfo.Timestamp);

            var id = await _connection.QuerySingleAsync<int>(sql, parameters);
            crowdInfo.Id = id;
            return crowdInfo;
        }

        public async Task<CrowdInfoDTO> CreateManualCriticalAlertAsync(int placeId, string? reason, string? source, CancellationToken ct = default)
        {
            const string sql = "dbo.sp_CrowdInfo_ManualCriticalAlert";

            var parameters = new DynamicParameters();
            parameters.Add("@PlaceId", placeId, DbType.Int32);
            parameters.Add("@Reason", reason, DbType.String, size: 256);
            parameters.Add("@Source", source ?? "ManualButton", DbType.String, size: 32);

            return await _connection.QuerySingleAsync<CrowdInfoDTO>(
                new CommandDefinition(
                    sql,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: ct));
        }

        public CrowdInfo? UpdateCrowdInfo(CrowdInfo crowdInfo)
        {
            if (crowdInfo == null || crowdInfo.Id <= 0)
            {
                throw new ArgumentException("Invalid crowd info provided for update.", nameof(crowdInfo));
            }

            

            try
            {
                const string sql = @"UPDATE CrowdInfo
                                     SET LocationName = @LocationName,
                                         Latitude = @Latitude,
                                         Longitude = @Longitude,
                                         CrowdLevel = @CrowdLevel,
                                         Timestamp = @Timestamp
                                     WHERE Id = @Id AND Active = 1";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Id", crowdInfo.Id);
                parameters.Add("@LocationName", crowdInfo.LocationName);
                parameters.Add("@Latitude", crowdInfo.Latitude);
                parameters.Add("@Longitude", crowdInfo.Longitude);
                parameters.Add("@CrowdLevel", crowdInfo.CrowdLevel);
                parameters.Add("@Timestamp", crowdInfo.Timestamp);

                var affectedRows = _connection.Execute(sql, parameters);

                if (affectedRows == 0)
                {
                    return null;
                }
                return crowdInfo;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error updating CrowdInfo: {ex.Message}");
            }
            return null;
        }
        public async Task<int> ArchivePastCrowdInfosAsync(CancellationToken ct = default)
        {
            try
            {
                var archived = await _connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        "dbo.sp_ArchivePastCrowdInfo",
                        new { MaxAgeMinutes = 30 },
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: ct));

                _logger.LogInformation("{Count} CrowdInfo archived.", archived);
                return archived;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving expired CrowdInfo.");
                return 0;
            }
        }

        public async Task<CrowdInfo?> UpsertCrowdInfoAsync(CrowdInfo input, CancellationToken ct = default)
        {
            const string sql = @"EXEC dbo.sp_CrowdInfo_Upsert
                            @LocationName, @Latitude, @Longitude, @CrowdLevel, @Timestamp;";

            var parameters = new DynamicParameters();
            parameters.Add("@LocationName", input.LocationName);
            parameters.Add("@Latitude", input.Latitude);
            parameters.Add("@Longitude", input.Longitude);
            parameters.Add("@CrowdLevel", input.CrowdLevel);
            parameters.Add("@Timestamp", input.Timestamp);

            return await _connection.QuerySingleOrDefaultAsync<CrowdInfo>(
                new CommandDefinition(sql, parameters, cancellationToken: ct));
        }
    }
}





















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.