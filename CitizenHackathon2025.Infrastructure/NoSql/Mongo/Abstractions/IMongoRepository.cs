using MongoDB.Bson;
using MongoDB.Driver;

namespace CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions
{
    public interface IMongoRepository<TDocument>
       where TDocument : class
    {
        Task InsertAsync(TDocument document, CancellationToken ct = default);

        Task InsertManyAsync(IEnumerable<TDocument> documents, CancellationToken ct = default);

        Task<IReadOnlyList<TDocument>> GetLatestAsync(
            string sortField,
            int limit = 50,
            CancellationToken ct = default);

        Task<IReadOnlyList<TDocument>> FindAsync(
            FilterDefinition<TDocument> filter,
            int limit = 100,
            CancellationToken ct = default);

        Task DeleteAsync(
            FilterDefinition<TDocument> filter,
            CancellationToken ct = default);
    }
}






























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.