using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CitizenHackathon2025.Infrastructure.NoSql.Mongo
{
    public interface IMongoDbContext
    {
        IMongoDatabase Database { get; }
        IMongoCollection<T> Collection<T>(string name);
    }

}
