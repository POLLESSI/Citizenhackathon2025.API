using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Collections;
using MongoDB.Driver;

namespace CitizenHackathon2025.Infrastructure.NoSql.Mongo.Repositories
{
    public sealed class CrowdSnapshotRepository : ICrowdSnapshotRepository
    {
        private readonly IMongoCollection<CrowdSnapshotDocument> _collection;

        public CrowdSnapshotRepository(IMongoDbContext context)
        {
            _collection = context.Collection<CrowdSnapshotDocument>("crowd_snapshots");
        }

        public Task InsertAsync(CrowdSnapshotDocument document, CancellationToken ct = default)
        {
            return _collection.InsertOneAsync(document, cancellationToken: ct);
        }

        public async Task<IReadOnlyList<CrowdSnapshotDocument>> GetLatestAsync(
            int limit = 50,
            CancellationToken ct = default)
        {
            limit = Math.Clamp(limit, 1, 500);

            return await _collection
                .Find(_ => true)
                .SortByDescending(x => x.SnapshotAtUtc)
                .Limit(limit)
                .ToListAsync(ct);
        }
    }
}

























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.