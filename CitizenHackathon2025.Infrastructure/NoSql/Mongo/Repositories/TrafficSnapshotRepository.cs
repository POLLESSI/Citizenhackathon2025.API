using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Collections;
using MongoDB.Driver;

namespace CitizenHackathon2025.Infrastructure.NoSql.Mongo.Repositories
{
    public sealed class TrafficSnapshotRepository : ITrafficSnapshotRepository
    {
        private readonly IMongoCollection<TrafficSnapshotDocument> _collection;

        public TrafficSnapshotRepository(IMongoDbContext context)
        {
            _collection = context.Collection<TrafficSnapshotDocument>("traffic_snapshots");
        }

        public Task InsertAsync(TrafficSnapshotDocument document, CancellationToken ct = default)
        {
            return _collection.InsertOneAsync(document, cancellationToken: ct);
        }

        public async Task<IReadOnlyList<TrafficSnapshotDocument>> GetLatestAsync(
            int limit = 50,
            CancellationToken ct = default)
        {
            limit = Math.Clamp(limit, 1, 500);

            return await _collection
                .Find(_ => true)
                .SortByDescending(x => x.CreatedAtUtc)
                .Limit(limit)
                .ToListAsync(ct);
        }
    }
}










































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.