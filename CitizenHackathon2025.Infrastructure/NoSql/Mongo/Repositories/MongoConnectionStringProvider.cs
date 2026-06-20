using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Options;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.Infrastructure.NoSql.Mongo.Repositories
{
    public sealed class MongoConnectionStringProvider : IMongoConnectionStringProvider
    {
        private readonly MongoOptions _options;

        public MongoConnectionStringProvider(IOptions<MongoOptions> options)
        {
            _options = options.Value;
        }

        public string GetConnectionString()
        {
            if (!string.IsNullOrWhiteSpace(_options.ConnectionString))
                return _options.ConnectionString;

            throw new InvalidOperationException(
                "MongoDb:ConnectionString is missing. In production, inject it via Key Vault reference or environment variable MongoDb__ConnectionString.");
        }
    }
}



























































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.