using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public class UserMessageAdminQueueRepository : IUserMessageAdminQueueRepository
    {
#nullable disable
        private readonly IDbConnection _connection;
        private readonly ILogger<UserMessageAdminQueueRepository> _logger;

        public UserMessageAdminQueueRepository(IDbConnection connection, ILogger<UserMessageAdminQueueRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<int> CreateAsync(UserMessageAdminQueue item, CancellationToken ct = default)
        {
            const string sql = """
                            IF NOT EXISTS
                            (
                                SELECT 1
                                FROM dbo.UserMessageAdminQueue
                                WHERE MessageId = @MessageId
                            )
                            BEGIN
                                INSERT INTO dbo.UserMessageAdminQueue
                                (
                                    MessageId,
                                    Category,
                                    Priority,
                                    Status,
                                    Confidence,
                                    ClassificationSource,
                                    Active
                                )
                                VALUES
                                (
                                    @MessageId,
                                    @Category,
                                    @Priority,
                                    @Status,
                                    @Confidence,
                                    @ClassificationSource,
                                    1
                                );

                                SELECT CAST(SCOPE_IDENTITY() AS int);
                            END
                            ELSE
                            BEGIN
                                SELECT Id
                                FROM dbo.UserMessageAdminQueue
                                WHERE MessageId = @MessageId;
                            END
                            """;

            return await _connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        item.MessageId,
                        Category = item.Category.ToString(),
                        item.Priority,
                        Status = item.Status.ToString(),
                        item.Confidence,
                        item.ClassificationSource
                    },
                    cancellationToken: ct));
        }

        public async Task<IReadOnlyList<AdminMessageQueueDto>> GetAsync(AdminMessageQueueFilter filter, CancellationToken ct = default)
        {
            filter ??= new AdminMessageQueueFilter();

            const string sql = """
                            SELECT TOP (@Take)

                                q.Id AS QueueId,
                                q.MessageId,

                                m.Content,
                                m.RelatedName,
                                m.SourceType,
                                m.Latitude,
                                m.Longitude,

                                q.Category,
                                q.Priority,
                                q.Status,
                                q.Confidence,
                                q.ClassificationSource,
                                q.AssignedTo,
                                q.AdminNote,

                                m.CreatedAt AS MessageCreatedAt,
                                q.CreatedAtUtc AS QueueCreatedAtUtc

                            FROM dbo.UserMessageAdminQueue q

                            INNER JOIN dbo.UserMessage m
                                ON m.Id = q.MessageId

                            WHERE q.Active = 1
                              AND m.Active = 1

                              AND
                              (
                                  @Status IS NULL
                                  OR q.Status = @Status
                              )

                              AND
                              (
                                  @Category IS NULL
                                  OR q.Category = @Category
                              )

                              AND
                              (
                                  @Priority IS NULL
                                  OR q.Priority = @Priority
                              )

                            ORDER BY
                                CASE q.Status
                                    WHEN N'Open'      THEN 0
                                    WHEN N'Reviewing' THEN 1
                                    WHEN N'Accepted'  THEN 2
                                    WHEN N'Resolved'  THEN 3
                                    WHEN N'Rejected'  THEN 4
                                    WHEN N'Ignored'   THEN 5
                                    ELSE 6
                                END,

                                q.Priority DESC,
                                q.CreatedAtUtc DESC;
                            """;

            var rows = await _connection.QueryAsync<AdminMessageQueueDto>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        Status = string.IsNullOrWhiteSpace(filter.Status) ? null : filter.Status.Trim(),
                        Category = string.IsNullOrWhiteSpace(filter.Category) ? null : filter.Category.Trim(),
                        filter.Priority, Take = Math.Clamp(filter.Take, 1, 500)
                    },
                    cancellationToken: ct));

            return rows.ToList();
        }
        public async Task<bool> UpdateStatusAsync(int id, AdminMessageStatus status, string? adminNote, string? assignedTo, CancellationToken ct = default)
        {
            if (id <= 0)
                return false;

            const string sql = """
                            UPDATE dbo.UserMessageAdminQueue

                            SET
                                Status = @Status,
                                AdminNote = @AdminNote,
                                AssignedTo = @AssignedTo,
                                UpdatedAtUtc = SYSUTCDATETIME(),
                                ResolvedAtUtc = CASE
                                    WHEN @Status IN
                                    (
                                        N'Resolved',
                                        N'Rejected',
                                        N'Ignored'
                                    )
                                    THEN
                                        COALESCE(ResolvedAtUtc, SYSUTCDATETIME())

                                    ELSE NULL
                                END

                            WHERE Id = @Id
                              AND Active = 1;
                            """;

            var affected = await _connection.ExecuteAsync(
                    new CommandDefinition(sql,
                        new
                        {
                            Id = id,
                            Status = status.ToString(),
                            AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim(),
                            AssignedTo = string.IsNullOrWhiteSpace(assignedTo) ? null : assignedTo.Trim()
                        },
                        cancellationToken: ct));

            return affected > 0;
        }
    }
}


































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.