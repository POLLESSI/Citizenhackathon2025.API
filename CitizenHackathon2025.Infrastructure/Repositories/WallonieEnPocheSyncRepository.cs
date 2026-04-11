using CitizenHackathon2025.Application.Models;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Contracts.DTOs;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public sealed class WallonieEnPocheSyncRepository : IWallonieEnPocheSyncRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<WallonieEnPocheSyncRepository> _logger;

        public WallonieEnPocheSyncRepository(
            IDbConnection connection,
            ILogger<WallonieEnPocheSyncRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<int?> ResolvePlaceIdByExternalAsync(
            string externalSource,
            string externalId,
            CancellationToken ct = default)
        {
            const string sql = @"
                            SELECT TOP(1) Id
                            FROM dbo.Place
                            WHERE ExternalSource = @ExternalSource
                              AND ExternalId = @ExternalId;";

            var cmd = new CommandDefinition(sql, new
            {
                ExternalSource = externalSource,
                ExternalId = externalId
            }, cancellationToken: ct);

            return await _connection.QueryFirstOrDefaultAsync<int?>(cmd);
        }

        public async Task<UpsertPlaceResult> UpsertPlaceAsync(
            WepPlaceImportDTO dto,
            string externalSource,
            CancellationToken ct = default)
        {
            const string sql = @"
                            DECLARE @Changes TABLE(ActionName NVARCHAR(10));

                            MERGE dbo.Place AS tgt
                            USING (
                                SELECT
                                    @ExternalSource AS ExternalSource,
                                    @ExternalId AS ExternalId,
                                    @Name AS Name,
                                    @Type AS [Type],
                                    @Indoor AS Indoor,
                                    @Latitude AS Latitude,
                                    @Longitude AS Longitude,
                                    @Capacity AS Capacity,
                                    @Tag AS Tag,
                                    @Active AS Active,
                                    @SourceUpdatedAtUtc AS SourceUpdatedAtUtc
                            ) AS src
                            ON tgt.ExternalSource = src.ExternalSource
                            AND tgt.ExternalId = src.ExternalId

                            WHEN MATCHED AND (
                                    ISNULL(tgt.Name, '') <> ISNULL(src.Name, '')
                                OR ISNULL(tgt.[Type], '') <> ISNULL(src.[Type], '')
                                OR ISNULL(tgt.Indoor, 0) <> ISNULL(src.Indoor, 0)
                                OR ISNULL(tgt.Latitude, 0) <> ISNULL(src.Latitude, 0)
                                OR ISNULL(tgt.Longitude, 0) <> ISNULL(src.Longitude, 0)
                                OR ISNULL(tgt.Capacity, 0) <> ISNULL(src.Capacity, 0)
                                OR ISNULL(tgt.Tag, '') <> ISNULL(src.Tag, '')
                                OR ISNULL(tgt.Active, 0) <> ISNULL(src.Active, 0)
                                OR ISNULL(tgt.SourceUpdatedAtUtc, '19000101') <> ISNULL(src.SourceUpdatedAtUtc, '19000101')
                            )
                            THEN UPDATE SET
                                Name = src.Name,
                                [Type] = src.[Type],
                                Indoor = src.Indoor,
                                Latitude = src.Latitude,
                                Longitude = src.Longitude,
                                Capacity = src.Capacity,
                                Tag = src.Tag,
                                Active = src.Active,
                                SourceUpdatedAtUtc = src.SourceUpdatedAtUtc

                            WHEN NOT MATCHED THEN
                                INSERT (Name, [Type], Indoor, Latitude, Longitude, Capacity, Tag, Active, ExternalSource, ExternalId, SourceUpdatedAtUtc)
                                VALUES (src.Name, src.[Type], src.Indoor, src.Latitude, src.Longitude, src.Capacity, src.Tag, src.Active, src.ExternalSource, src.ExternalId, src.SourceUpdatedAtUtc)

                            OUTPUT $action INTO @Changes;

                            SELECT TOP(1)
                                p.Id,
                                p.Name,
                                p.[Type],
                                p.Indoor,
                                p.Latitude,
                                p.Longitude,
                                p.Capacity,
                                p.Tag,
                                p.Active,
                                p.ExternalSource,
                                p.ExternalId,
                                p.SourceUpdatedAtUtc,
                                ISNULL((SELECT TOP 1 ActionName FROM @Changes), 'NONE') AS MergeAction
                            FROM dbo.Place p
                            WHERE p.ExternalSource = @ExternalSource
                                AND p.ExternalId = @ExternalId;";

            var cmd = new CommandDefinition(sql, new
            {
                ExternalSource = externalSource,
                dto.ExternalId,
                dto.Name,
                dto.Type,
                Indoor = dto.Indoor ?? false,
                dto.Latitude,
                dto.Longitude,
                Capacity = dto.Capacity ?? 0,
                dto.Tag,
                Active = dto.IsActive,
                dto.SourceUpdatedAtUtc
            }, cancellationToken: ct);

            var row = await _connection.QuerySingleAsync<PlaceMergeRow>(cmd);

            return new UpsertPlaceResult
            {
                Entity = new Place
                {
                    Id = row.Id,
                    Name = row.Name,
                    Type = row.Type,
                    Indoor = row.Indoor,
                    Latitude = row.Latitude,
                    Longitude = row.Longitude,
                    Capacity = row.Capacity,
                    Tag = row.Tag,
                    ExternalSource = row.ExternalSource,
                    ExternalId = row.ExternalId,
                    SourceUpdatedAtUtc = row.SourceUpdatedAtUtc,
                    //Active = row.Active,
                },
                Inserted = row.MergeAction == "INSERT",
                Updated = row.MergeAction == "UPDATE",
                Skipped = row.MergeAction == "NONE"
            };
        }

        public async Task<UpsertEventResult> UpsertEventAsync(
            WepEventImportDTO dto,
            string externalSource,
            CancellationToken ct = default)
        {
            int? placeId = null;

            if (!string.IsNullOrWhiteSpace(dto.PlaceExternalId))
            {
                placeId = await ResolvePlaceIdByExternalAsync(externalSource, dto.PlaceExternalId, ct);
            }

            const string sql = @"
                            DECLARE @Changes TABLE(ActionName NVARCHAR(10));

                            MERGE dbo.Event AS tgt
                            USING (
                                SELECT
                                    @ExternalSource AS ExternalSource,
                                    @ExternalId AS ExternalId,
                                    @Name AS Name,
                                    @PlaceId AS PlaceId,
                                    @Latitude AS Latitude,
                                    @Longitude AS Longitude,
                                    @DateEvent AS DateEvent,
                                    @ExpectedCrowd AS ExpectedCrowd,
                                    @IsOutdoor AS IsOutdoor,
                                    @Active AS Active,
                                    @SourceUpdatedAtUtc AS SourceUpdatedAtUtc
                            ) AS src
                            ON tgt.ExternalSource = src.ExternalSource
                            AND tgt.ExternalId = src.ExternalId

                            WHEN MATCHED AND (
                                   ISNULL(tgt.Name, '') <> ISNULL(src.Name, '')
                                OR ISNULL(tgt.PlaceId, -1) <> ISNULL(src.PlaceId, -1)
                                OR ISNULL(tgt.Latitude, 0) <> ISNULL(src.Latitude, 0)
                                OR ISNULL(tgt.Longitude, 0) <> ISNULL(src.Longitude, 0)
                                OR ISNULL(tgt.DateEvent, '19000101') <> ISNULL(src.DateEvent, '19000101')
                                OR ISNULL(tgt.ExpectedCrowd, 0) <> ISNULL(src.ExpectedCrowd, 0)
                                OR ISNULL(tgt.IsOutdoor, 0) <> ISNULL(src.IsOutdoor, 0)
                                OR ISNULL(tgt.Active, 0) <> ISNULL(src.Active, 0)
                                OR ISNULL(tgt.SourceUpdatedAtUtc, '19000101') <> ISNULL(src.SourceUpdatedAtUtc, '19000101')
                            )
                            THEN UPDATE SET
                                Name = src.Name,
                                PlaceId = src.PlaceId,
                                Latitude = src.Latitude,
                                Longitude = src.Longitude,
                                DateEvent = src.DateEvent,
                                ExpectedCrowd = src.ExpectedCrowd,
                                IsOutdoor = src.IsOutdoor,
                                Active = src.Active,
                                SourceUpdatedAtUtc = src.SourceUpdatedAtUtc

                            WHEN NOT MATCHED THEN
                                INSERT (Name, PlaceId, Latitude, Longitude, DateEvent, ExpectedCrowd, IsOutdoor, Active, ExternalSource, ExternalId, SourceUpdatedAtUtc)
                                VALUES (src.Name, src.PlaceId, src.Latitude, src.Longitude, src.DateEvent, src.ExpectedCrowd, src.IsOutdoor, src.Active, src.ExternalSource, src.ExternalId, src.SourceUpdatedAtUtc)

                            OUTPUT $action INTO @Changes;

                            SELECT TOP(1)
                                e.Id,
                                e.Name,
                                e.PlaceId,
                                e.Latitude,
                                e.Longitude,
                                e.DateEvent,
                                e.ExpectedCrowd,
                                e.IsOutdoor,
                                e.Active,
                                e.ExternalSource,
                                e.ExternalId,
                                e.SourceUpdatedAtUtc,
                                ISNULL((SELECT TOP 1 ActionName FROM @Changes), 'NONE') AS MergeAction
                            FROM dbo.Event e
                            WHERE e.ExternalSource = @ExternalSource
                              AND e.ExternalId = @ExternalId;";

            var cmd = new CommandDefinition(sql, new
            {
                ExternalSource = externalSource,
                dto.ExternalId,
                dto.Name,
                PlaceId = placeId,
                dto.Latitude,
                dto.Longitude,
                dto.DateEvent,
                dto.ExpectedCrowd,
                IsOutdoor = dto.IsOutdoor ?? false,
                Active = dto.IsActive,
                dto.SourceUpdatedAtUtc
            }, cancellationToken: ct);

            var row = await _connection.QuerySingleAsync<EventMergeRow>(cmd);

            return new UpsertEventResult
            {
                Entity = new Event
                {
                    Id = row.Id,
                    Name = row.Name,
                    PlaceId = row.PlaceId,
                    Latitude = row.Latitude,
                    Longitude = row.Longitude,
                    DateEvent = row.DateEvent,
                    ExpectedCrowd = row.ExpectedCrowd,
                    IsOutdoor = row.IsOutdoor,
                    Active = row.Active,
                    ExternalSource = row.ExternalSource,
                    ExternalId = row.ExternalId,
                    SourceUpdatedAtUtc = row.SourceUpdatedAtUtc
                },
                Inserted = row.MergeAction == "INSERT",
                Updated = row.MergeAction == "UPDATE",
                Skipped = row.MergeAction == "NONE"
            };
        }

        private sealed class PlaceMergeRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Type { get; set; } = "";
            public bool Indoor { get; set; }
            public decimal Latitude { get; set; }
            public decimal Longitude { get; set; }
            public int Capacity { get; set; }
            public string Tag { get; set; } = "";
            public bool Active { get; set; }
            public string? ExternalSource { get; set; }
            public string? ExternalId { get; set; }
            public DateTime? SourceUpdatedAtUtc { get; set; }
            public string MergeAction { get; set; } = "NONE";
        }

        private sealed class EventMergeRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public int? PlaceId { get; set; }
            public decimal Latitude { get; set; }
            public decimal Longitude { get; set; }
            public DateTime DateEvent { get; set; }
            public int? ExpectedCrowd { get; set; }
            public bool IsOutdoor { get; set; }
            public bool Active { get; set; }
            public string? ExternalSource { get; set; }
            public string? ExternalId { get; set; }
            public DateTime? SourceUpdatedAtUtc { get; set; }
            public string MergeAction { get; set; } = "NONE";
        }
    }
}


































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.