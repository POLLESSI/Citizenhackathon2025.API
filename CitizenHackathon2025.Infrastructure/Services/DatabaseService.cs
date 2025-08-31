using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class DatabaseService
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(IDbConnection connection, ILogger<DatabaseService> logger)
        {
            _connection = connection;
            _logger = logger;
        }
        /// <summary>
        /// Checks connectivity to the database.
        /// </summary>
        public async Task<bool> IsDatabaseAvailableAsync()
        {
            try
            {
                const string sql = "SELECT 1";
                var result = await _connection.ExecuteScalarAsync<int>(sql);
                return result == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connectivity error");
                return false;
            }
        }

        /// <summary>
        /// Executes an arbitrary SQL command (⚠️ beware of injections!)
        /// </summary>
        //public async Task<int> ExecuteCommandAsync(string sql)
        //{
        //    try
        //    {
        //        return await _connection.ExecuteAsync(sql);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erreur lors de l'exécution de la commande SQL");
        //        throw;
        //    }
        //}

        /// <summary>
        /// Empty a table (useful for maintenance scripts).
        /// </summary>
        //public async Task<int> TruncateTableAsync(string tableName)
        //{
        //    if (string.IsNullOrWhiteSpace(tableName))
        //        throw new ArgumentException("Le nom de la table est requis.");

        //    string sql = $"TRUNCATE TABLE [{tableName}]";
        //    //return await ExecuteCommandAsync(sql);
        //}
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.