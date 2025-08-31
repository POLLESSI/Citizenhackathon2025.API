using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public class CrowdInfoRepository : ICrowdInfoRepository
    {
    #nullable disable
        private readonly System.Data.IDbConnection _connection;

        public CrowdInfoRepository(System.Data.IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<bool> DeleteCrowdInfoAsync(int id)
        {
            var sql = "DELETE FROM CrowdInfo WHERE Id = @Id";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            var affectedRows = await _connection.ExecuteAsync(sql, parameters);
            return affectedRows > 0;
        }

        public async Task<IEnumerable<CrowdInfo>> GetAllCrowdInfoAsync()
        {
            var sql = "SELECT Id, LocationName, Latitude, Longitude, CrowdLevel, Timestamp FROM CrowdInfo WHERE Active = 1 ORDER BY LocationName DESC";
            var result = await _connection.QueryAsync<CrowdInfo>(sql);
            return result;
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
            var sql = @"
                INSERT INTO CrowdInfo (LocationName, Latitude, Longitude, CrowdLevel, Timestamp)
                VALUES (@LocationName, @Latitude, @Longitude, @CrowdLevel, @Timestamp);
                SELECT CAST(SCOPE_IDENTITY() as int);
            ";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@LocationName", crowdInfo.LocationName);
            parameters.Add("@Latitude", crowdInfo.Latitude);
            parameters.Add("@Longitude", crowdInfo.Longitude);
            parameters.Add("@CrowdLevel", crowdInfo.CrowdLevel);
            parameters.Add("@Timestamp", crowdInfo.Timestamp);

            var id = await _connection.QuerySingleAsync<int>(sql, parameters);
            crowdInfo.Id = id;
            return crowdInfo;
        }

        public CrowdInfo UpdateCrowdInfo(CrowdInfo crowdInfo)
        {
            if (crowdInfo == null || crowdInfo.Id <= 0)
            {
                throw new ArgumentException("Invalid crowd info provided for update.", nameof(crowdInfo));
            }

            

            try
            {
                string sql = "UPDATE CrowdInfo SET LocationName = @LocationName, Latitude = CAST(@Latitude AS DECIMAL(8, 6)), Longitude = CAST(@Longitude AS DECIMAL(9, 6)), CrowdLevel = @CrowdLevel, Timestamp = @Timestamp WHERE Id = @Id AND Active = 1";
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
    }
}





















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.