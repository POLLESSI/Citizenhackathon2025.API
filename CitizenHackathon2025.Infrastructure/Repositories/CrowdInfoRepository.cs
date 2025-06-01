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
            var sql = "SELECT * FROM CrowdInfo";
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
        // Implement other methods as needed
    }
}
