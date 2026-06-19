using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Collections;
using MongoDB.Driver;

namespace CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions
{
    public interface IGptInteractionNoSqlRepository
    {
        Task InsertAsync(GptInteractionDocument document, CancellationToken ct = default);
        Task<IReadOnlyList<GptInteractionDocument>> GetLatestAsync(int limit, CancellationToken ct = default);
    }
}



































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.