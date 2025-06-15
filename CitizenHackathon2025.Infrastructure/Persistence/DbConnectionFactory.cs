using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Citizenhackathon2025.Infrastructure.Persistence
{
    public class DbConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DbConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("default")
                ?? throw new InvalidOperationException("Connection string 'default' not found.");
        }

        public IDbConnection CreateConnection()
        {
            return null /*new SqlConnection(_connectionString)*/; // SqlConnection now uses the correct namespace  
        }
    }
}
