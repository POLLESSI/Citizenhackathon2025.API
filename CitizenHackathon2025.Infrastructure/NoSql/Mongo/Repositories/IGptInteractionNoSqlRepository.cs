using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Collections;
using MongoDB.Driver;

namespace CitizenHackathon2025.Infrastructure.NoSql.Mongo.Repositories
{
    public interface IGptInteractionNoSqlRepository
    {
        Task InsertAsync(GptInteractionDocument document, CancellationToken ct = default);
        Task<IReadOnlyList<GptInteractionDocument>> GetLatestAsync(int limit, CancellationToken ct = default);
    }
}
