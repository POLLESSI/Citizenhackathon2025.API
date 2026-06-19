using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions
{
    public interface IMongoDbContext
    {
        IMongoDatabase Database { get; }
        IMongoCollection<T> Collection<T>(string name);
    }

}









































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.