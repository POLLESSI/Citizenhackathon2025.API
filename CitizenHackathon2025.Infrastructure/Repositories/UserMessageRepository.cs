using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using System.Data;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public class UserMessageRepository : IUserMessageRepository
    {
        private readonly IDbConnection _db;

        public UserMessageRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<UserMessage> InsertAsync(UserMessage msg, CancellationToken ct = default)
        {
            const string sql = @"
                            INSERT INTO dbo.UserMessage (UserId, SourceType, SourceId, RelatedName, Latitude, Longitude, Tags, Content)
                            OUTPUT INSERTED.*
                            VALUES (@UserId, @SourceType, @SourceId, @RelatedName, @Latitude, @Longitude, @Tags, @Content);";

            return await _db.QuerySingleAsync<UserMessage>(new CommandDefinition(sql, msg, cancellationToken: ct));
        }

        public async Task<List<UserMessage>> GetLatestAsync(int take = 100, CancellationToken ct = default)
        {
            const string sql = @"
                            SELECT TOP (@Take) * 
                            FROM dbo.UserMessage
                            WHERE Active = 1
                            ORDER BY CreatedAt DESC;";

            var rows = await _db.QueryAsync<UserMessage>(new CommandDefinition(sql, new { Take = take }, cancellationToken: ct));
            return rows.ToList();
        }

        public async Task<UserMessage?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            const string sql = @"SELECT * FROM dbo.UserMessage WHERE Id = @Id AND Active = 1;";
            return await _db.QuerySingleOrDefaultAsync<UserMessage>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        }

        public async Task<bool> DeleteMessageAsync(int id, CancellationToken ct = default)
        {
            const string sql = @"
        DELETE FROM dbo.UserMessage
        WHERE Id = @Id AND Active = 1;";

            // The INSTEAD OF DELETE trigger will convert this DELETE into UPDATE Active=0
            var affected = await _db.ExecuteAsync(new CommandDefinition(
                sql,
                new { Id = id },
                cancellationToken: ct));

            return affected > 0;
        }
    }
}
