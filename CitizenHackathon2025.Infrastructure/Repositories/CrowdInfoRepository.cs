using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Interfaces;
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Citizenhackathon2025.Infrastructure.Repositories
{
    public class CrowdInfoRepository : ICrowdInfoRepository
    {
        private readonly System.Data.IDbConnection _dbConnection;

        public CrowdInfoRepository(System.Data.IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<bool> DeleteCrowdInfoAsync(int id)
        {
            var sql = "DELETE FROM CrowdInfo WHERE Id = @Id";
            var affectedRows = await _dbConnection.ExecuteAsync(sql, new { Id = id });
            return affectedRows > 0;
        }

        public async Task<IEnumerable<CrowdInfo>> GetAllCrowdInfoAsync()
        {
            var sql = "SELECT Id, LocationName, Latitude, Longitude, CrowdLevel, Timestamp FROM CrowdInfo WHERE Active = 1 ORDER BY LocationName DESC";
            var result = await _dbConnection.QueryAsync<CrowdInfo>(sql);
            return result;
        }

        public async Task<CrowdInfo?> GetCrowdInfoByIdAsync(int id)
        {
            var sql = "SELECT * FROM CrowdInfo WHERE Id = @Id";
            var result = await _dbConnection.QuerySingleOrDefaultAsync<CrowdInfo>(sql, new { Id = id });
            return result;
        }

        public async Task<CrowdInfo?> SaveCrowdInfoAsync(CrowdInfo crowdInfo)
        {
            var sql = @"
                INSERT INTO CrowdInfo (LocationName, Latitude, Longitude, CrowdLevel, Timestamp)
                VALUES (@LocationName, @Latitude, @Longitude, @CrowdLevel, @Timestamp);
                SELECT CAST(SCOPE_IDENTITY() as int);
            ";

            var id = await _dbConnection.QuerySingleAsync<int>(sql, crowdInfo);
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

                var affectedRows = _dbConnection.Execute(sql, parameters);

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
