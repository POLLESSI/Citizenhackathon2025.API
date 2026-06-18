using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using System.Data;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public sealed class DisasterAlertRepository : IDisasterAlertRepository
    {
        private readonly IDbConnection _db;

        public DisasterAlertRepository(IDbConnection db)
        {
            _db = db;
        }

        public Task<DisasterAlert> InsertAsync(DisasterAlert alert, CancellationToken ct = default)
        {
            const string sql = """
                INSERT INTO dbo.DisasterAlert
                (
                    DisasterType,
                    Severity,
                    Latitude,
                    Longitude,
                    PlaceName,
                    Description,
                    ConfirmationCount,
                    RequiredCount,
                    Status,
                    ExpiresAtUtc,
                    Active
                )
                OUTPUT
                    inserted.Id,
                    inserted.DisasterType,
                    inserted.Severity,
                    inserted.Latitude,
                    inserted.Longitude,
                    inserted.PlaceName,
                    inserted.Description,
                    inserted.ConfirmationCount,
                    inserted.RequiredCount,
                    inserted.Status,
                    inserted.CreatedAtUtc,
                    inserted.ExpiresAtUtc,
                    inserted.Active
                VALUES
                (
                    @DisasterType,
                    @Severity,
                    @Latitude,
                    @Longitude,
                    @PlaceName,
                    @Description,
                    @ConfirmationCount,
                    @RequiredCount,
                    @Status,
                    @ExpiresAtUtc,
                    @Active
                );
                """;

            return _db.QuerySingleAsync<DisasterAlert>(
                new CommandDefinition(sql, alert, cancellationToken: ct));
        }

        public Task<EmergencyEscalationRequest> InsertEscalationAsync(
            EmergencyEscalationRequest request,
            CancellationToken ct = default)
        {
            const string sql = """
                INSERT INTO dbo.EmergencyEscalationRequest
                (
                    DisasterAlertId,
                    TargetService,
                    Status,
                    PayloadJson,
                    ReviewedByUserId
                )
                OUTPUT
                    inserted.Id,
                    inserted.DisasterAlertId,
                    inserted.TargetService,
                    inserted.Status,
                    inserted.PayloadJson,
                    inserted.CreatedAtUtc,
                    inserted.SentAtUtc,
                    inserted.ReviewedByUserId
                VALUES
                (
                    @DisasterAlertId,
                    @TargetService,
                    @Status,
                    @PayloadJson,
                    @ReviewedByUserId
                );
                """;

            return _db.QuerySingleAsync<EmergencyEscalationRequest>(
                new CommandDefinition(sql, request, cancellationToken: ct));
        }
    }
}






















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.