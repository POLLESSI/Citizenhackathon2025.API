using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CitizenHackathon2025.Infrastructure.NoSql.Mongo.Repositories
{
    public sealed class MongoRepository<TDocument> : IMongoRepository<TDocument>
        where TDocument : class
    {
        private readonly IMongoCollection<TDocument> _collection;

        public MongoRepository(IMongoDbContext context, string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("Collection name is required.", nameof(collectionName));

            _collection = context.Collection<TDocument>(collectionName);
        }

        public Task InsertAsync(TDocument document, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(document);

            return _collection.InsertOneAsync(document, cancellationToken: ct);
        }

        public Task InsertManyAsync(IEnumerable<TDocument> documents, CancellationToken ct = default)
        {
            var list = documents?.ToList() ?? throw new ArgumentNullException(nameof(documents));

            if (list.Count == 0)
                return Task.CompletedTask;

            return _collection.InsertManyAsync(list, cancellationToken: ct);
        }

        public async Task<IReadOnlyList<TDocument>> GetLatestAsync(
            string sortField,
            int limit = 50,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sortField))
                throw new ArgumentException("Sort field is required.", nameof(sortField));

            limit = Math.Clamp(limit, 1, 500);

            var sort = Builders<TDocument>.Sort.Descending(sortField);

            return await _collection
                .Find(FilterDefinition<TDocument>.Empty)
                .Sort(sort)
                .Limit(limit)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<TDocument>> FindAsync(
            FilterDefinition<TDocument> filter,
            int limit = 100,
            CancellationToken ct = default)
        {
            limit = Math.Clamp(limit, 1, 500);

            return await _collection
                .Find(filter)
                .Limit(limit)
                .ToListAsync(ct);
        }

        public Task DeleteAsync(
            FilterDefinition<TDocument> filter,
            CancellationToken ct = default)
        {
            return _collection.DeleteManyAsync(filter, ct);
        }
    }
}











































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.