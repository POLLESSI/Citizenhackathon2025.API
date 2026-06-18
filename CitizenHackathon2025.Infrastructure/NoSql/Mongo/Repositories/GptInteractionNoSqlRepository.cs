using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Collections;
using MongoDB.Driver;

namespace CitizenHackathon2025.Infrastructure.NoSql.Mongo.Repositories
{
    public sealed class GptInteractionNoSqlRepository : IGptInteractionNoSqlRepository
    {
        private readonly IMongoCollection<GptInteractionDocument> _collection;

        public GptInteractionNoSqlRepository(IMongoDbContext context)
        {
            _collection = context.Collection<GptInteractionDocument>("gpt_interactions");
        }

        public Task InsertAsync(GptInteractionDocument document, CancellationToken ct = default)
            => _collection.InsertOneAsync(document, cancellationToken: ct);

        public async Task<IReadOnlyList<GptInteractionDocument>> GetLatestAsync(int limit, CancellationToken ct = default)
        {
            return await _collection
                .Find(_ => true)
                .SortByDescending(x => x.CreatedAtUtc)
                .Limit(limit)
                .ToListAsync(ct);
        }
    }
}
