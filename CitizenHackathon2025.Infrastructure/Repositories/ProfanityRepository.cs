using CitizenHackathon2025.Domain.Common;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using System.Data;
using System.Text;

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

        public async Task<PagedResultDto<ProfanityWord>> GetPagedAsync(
            int page,
            int pageSize,
            string? languageCode = null,
            string? search = null,
            CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var where = new StringBuilder("WHERE 1 = 1");
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(languageCode))
            {
                where.AppendLine(" AND LanguageCode = @LanguageCode");
                parameters.Add("LanguageCode", languageCode.Trim());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                where.AppendLine("""
                            AND (
                                Word LIKE @Search
                                OR NormalizedWord LIKE @Search
                                OR Category LIKE @Search
                            )
                            """);
                parameters.Add("Search", $"%{search.Trim()}%");
            }

            var countSql = $"""
                        SELECT COUNT(1)
                        FROM dbo.ProfanityWord
                        {where}
                        """;

            var totalCount = await _db.ExecuteScalarAsync<int>(
                new CommandDefinition(countSql, parameters, cancellationToken: ct));

            parameters.Add("Offset", (page - 1) * pageSize);
            parameters.Add("PageSize", pageSize);

            var dataSql = $"""
                        SELECT *
                        FROM dbo.ProfanityWord
                        {where}
                        ORDER BY Active DESC, LanguageCode, Weight DESC, Word ASC
                        OFFSET @Offset ROWS
                        FETCH NEXT @PageSize ROWS ONLY;
                        """;

            var items = await _db.QueryAsync<ProfanityWord>(
                new CommandDefinition(dataSql, parameters, cancellationToken: ct));

            var totalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PagedResultDto<ProfanityWord>
            {
                Items = items.ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
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
















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.