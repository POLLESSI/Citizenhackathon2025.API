using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using System.Data;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public sealed class ProfanityRepository : IProfanityRepository
    {
        private readonly IDbConnection _db;

        public ProfanityRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<ProfanityWord>> GetAllActiveAsync(CancellationToken ct = default)
        {
            const string sql = """
                SELECT *
                FROM dbo.ProfanityWord
                WHERE Active = 1
                ORDER BY LanguageCode, Weight DESC, Word ASC;
                """;

            var rows = await _db.QueryAsync<ProfanityWord>(
                new CommandDefinition(sql, cancellationToken: ct));

            return rows.ToList();
        }

        public async Task<IReadOnlyList<ProfanityWord>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            const string sql = """
                SELECT *
                FROM dbo.ProfanityWord
                ORDER BY Active DESC, LanguageCode, Weight DESC, Word ASC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
                """;

            var rows = await _db.QueryAsync<ProfanityWord>(
                new CommandDefinition(sql, new
                {
                    Offset = (page - 1) * pageSize,
                    PageSize = pageSize
                }, cancellationToken: ct));

            return rows.ToList();
        }

        public async Task<ProfanityWord?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            const string sql = """
                SELECT *
                FROM dbo.ProfanityWord
                WHERE Id = @Id;
                """;

            return await _db.QuerySingleOrDefaultAsync<ProfanityWord>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        }

        public async Task<ProfanityWord?> GetByWordAsync(string normalizedWord, CancellationToken ct = default)
        {
            const string sql = """
                SELECT TOP 1 *
                FROM dbo.ProfanityWord
                WHERE NormalizedWord = @NormalizedWord AND Active = 1;
                """;

            return await _db.QuerySingleOrDefaultAsync<ProfanityWord>(
                new CommandDefinition(sql, new { NormalizedWord = normalizedWord }, cancellationToken: ct));
        }

        public async Task<ProfanityWord> InsertAsync(ProfanityWord entity, CancellationToken ct = default)
        {
            const string sql = """
                INSERT INTO dbo.ProfanityWord
                (
                    Word,
                    NormalizedWord,
                    LanguageCode,
                    Weight,
                    IsRegex,
                    Category,
                    Active
                )
                OUTPUT INSERTED.*
                VALUES
                (
                    @Word,
                    @NormalizedWord,
                    @LanguageCode,
                    @Weight,
                    @IsRegex,
                    @Category,
                    @Active
                );
                """;

            return await _db.QuerySingleAsync<ProfanityWord>(
                new CommandDefinition(sql, entity, cancellationToken: ct));
        }

        public async Task<bool> UpdateAsync(ProfanityWord entity, CancellationToken ct = default)
        {
            const string sql = """
                UPDATE dbo.ProfanityWord
                SET
                    Word = @Word,
                    NormalizedWord = @NormalizedWord,
                    LanguageCode = @LanguageCode,
                    Weight = @Weight,
                    IsRegex = @IsRegex,
                    Category = @Category,
                    UpdatedAtUtc = SYSUTCDATETIME()
                WHERE Id = @Id;
                """;

            var affected = await _db.ExecuteAsync(
                new CommandDefinition(sql, entity, cancellationToken: ct));

            return affected > 0;
        }

        public async Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default)
        {
            const string sql = """
                UPDATE dbo.ProfanityWord
                SET
                    Active = 0,
                    UpdatedAtUtc = SYSUTCDATETIME()
                WHERE Id = @Id AND Active = 1;
                """;

            var affected = await _db.ExecuteAsync(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));

            return affected > 0;
        }

        public async Task<bool> SetActiveAsync(int id, bool active, CancellationToken ct = default)
        {
            const string sql = """
                UPDATE dbo.ProfanityWord
                SET
                    Active = @Active,
                    UpdatedAtUtc = SYSUTCDATETIME()
                WHERE Id = @Id;
                """;

            var affected = await _db.ExecuteAsync(
                new CommandDefinition(sql, new { Id = id, Active = active }, cancellationToken: ct));

            return affected > 0;
        }
    }
}