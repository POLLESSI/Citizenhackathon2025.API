using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Collections;
using MongoDB.Driver;

namespace CitizenHackathon2025.Infrastructure.NoSql.Mongo.Repositories
{
    public sealed class SignalRDiagnosticRepository : ISignalRDiagnosticRepository
    {
        private readonly IMongoCollection<SignalRDiagnosticDocument> _collection;

        public SignalRDiagnosticRepository(IMongoDbContext context)
        {
            _collection = context.Collection<SignalRDiagnosticDocument>("signalr_diagnostics");
        }

        public Task InsertAsync(
            SignalRDiagnosticDocument document,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(document);

            return _collection.InsertOneAsync(document, cancellationToken: ct);
        }
    }
}













































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.