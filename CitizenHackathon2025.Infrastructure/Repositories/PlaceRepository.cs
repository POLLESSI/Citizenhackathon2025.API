using Dapper;
using CitizenHackathon2025.Domain.Interfaces;
using System.Data;
using CitizenHackathon2025.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using IDbConnection = System.Data.IDbConnection;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public class PlaceRepository : IPlaceRepository
    {
#nullable disable
        private readonly IDbConnection _connection;
        private readonly ILogger<PlaceRepository> _logger;

        public PlaceRepository(IDbConnection connection, ILogger<PlaceRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public Task<IEnumerable<Place?>> GetLatestPlaceAsync(int limit = 200, CancellationToken ct = default)
        {
            const string sql = @"
                            SELECT TOP(@Limit)
                                [Id],
                                [Name],
                                [Type],
                                [Indoor],
                                [Latitude],
                                [Longitude],
                                [Capacity],
                                [Tag],
                                [ExternalSource],
                                [ExternalId],
                                [SourceUpdatedAtUtc],
                                [Active]
                            FROM [Place]
                            WHERE [Active] = 1
                            ORDER BY Id DESC;";

            return _connection.QueryAsync<Place?>(
                new CommandDefinition(sql, new { Limit = limit }, cancellationToken: ct));
        }

        public async Task<IEnumerable<Place>> GetNearbyPlacesAsync(
            double latitude,
            double longitude,
            double radiusKm,
            CancellationToken ct = default)
        {
            const string sql = @"
                            WITH PlaceBase AS
                            (
                                SELECT
                                    p.Id,
                                    p.Name,
                                    p.Type,
                                    p.Indoor,
                                    p.Latitude,
                                    p.Longitude,
                                    p.Capacity,
                                    p.Tag,
                                    p.ExternalSource,
                                    p.ExternalId,
                                    p.SourceUpdatedAtUtc,
                                    p.Active,
                                    DistanceKm =
                                        6371.0 * ACOS(
                                            CASE
                                                WHEN
                                                    COS(RADIANS(@Lat)) * COS(RADIANS(CAST(p.Latitude AS float))) *
                                                    COS(RADIANS(CAST(p.Longitude AS float)) - RADIANS(@Lng)) +
                                                    SIN(RADIANS(@Lat)) * SIN(RADIANS(CAST(p.Latitude AS float))) > 1
                                                THEN 1
                                                WHEN
                                                    COS(RADIANS(@Lat)) * COS(RADIANS(CAST(p.Latitude AS float))) *
                                                    COS(RADIANS(CAST(p.Longitude AS float)) - RADIANS(@Lng)) +
                                                    SIN(RADIANS(@Lat)) * SIN(RADIANS(CAST(p.Latitude AS float))) < -1
                                                THEN -1
                                                ELSE
                                                    COS(RADIANS(@Lat)) * COS(RADIANS(CAST(p.Latitude AS float))) *
                                                    COS(RADIANS(CAST(p.Longitude AS float)) - RADIANS(@Lng)) +
                                                    SIN(RADIANS(@Lat)) * SIN(RADIANS(CAST(p.Latitude AS float)))
                                            END
                                        )
                                FROM dbo.Place p
                                WHERE p.Active = 1
                                  AND p.Name IS NOT NULL
                                  AND LTRIM(RTRIM(p.Name)) <> ''
                                  AND p.Latitude IS NOT NULL
                                  AND p.Longitude IS NOT NULL
                            )
                            SELECT TOP (12)
                                Id,
                                Name,
                                Type,
                                Indoor,
                                Latitude,
                                Longitude,
                                Capacity,
                                Tag,
                                ExternalSource,
                                ExternalId,
                                SourceUpdatedAtUtc,
                                Active
                            FROM PlaceBase
                            WHERE DistanceKm <= @RadiusKm
                            ORDER BY DistanceKm ASC, Capacity DESC, Name ASC;";

            var parameters = new DynamicParameters();
            parameters.Add("@Lat", latitude, DbType.Double);
            parameters.Add("@Lng", longitude, DbType.Double);
            parameters.Add("@RadiusKm", radiusKm, DbType.Double);

            try
            {
                var cmd = new CommandDefinition(sql, parameters, cancellationToken: ct);
                var result = await _connection.QueryAsync<Place>(cmd);
                return result ?? Enumerable.Empty<Place>();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving nearby places. Lat={Lat}, Lng={Lng}, RadiusKm={RadiusKm}",
                    latitude,
                    longitude,
                    radiusKm);

                return Enumerable.Empty<Place>();
            }
        }

        public async Task<Place> SavePlaceAsync(Place place, CancellationToken ct = default)
        {
            try
            {
                const string sql = @"
                                INSERT INTO [Place]
                                (
                                    [Name],
                                    [Type],
                                    [Indoor],
                                    [Latitude],
                                    [Longitude],
                                    [Capacity],
                                    [Tag]
                                )
                                VALUES
                                (
                                    @Name,
                                    @Type,
                                    @Indoor,
                                    @Latitude,
                                    @Longitude,
                                    @Capacity,
                                    @Tag
                                );";

                var parameters = new DynamicParameters();
                parameters.Add("@Name", place.Name);
                parameters.Add("@Type", place.Type);
                parameters.Add("@Indoor", place.Indoor);
                parameters.Add("@Latitude", place.Latitude);
                parameters.Add("@Longitude", place.Longitude);
                parameters.Add("@Capacity", place.Capacity);
                parameters.Add("@Tag", place.Tag);

                await _connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: ct));
                return place;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding Place.");
                return null;
            }
        }

        public async Task<Place> GetByIdAsync(int id, CancellationToken ct = default)
        {
            const string sql = @"
                            SELECT TOP(1) *
                            FROM dbo.Place
                            WHERE Id = @Id;";

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Id", id, DbType.Int32);

                var cmd = new CommandDefinition(sql, parameters, cancellationToken: ct);
                return await _connection.QueryFirstOrDefaultAsync<Place>(cmd);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Place by Id={Id}", id);
                return null;
            }
        }

        public Place UpdatePlace(Place place)
        {
            if (place == null || place.Id <= 0)
                throw new ArgumentException("Invalid place to update.", nameof(place));

            try
            {
                const string sql = @"
                                IF EXISTS (SELECT 1 FROM Place WHERE Name = @Name)
                                    UPDATE [Place]
                                       SET Type = @Type,
                                           Indoor = @Indoor,
                                           Latitude = @Latitude,
                                           Longitude = @Longitude,
                                           Capacity = @Capacity,
                                           Tag = @Tag
                                     WHERE Name = @Name;
                                ELSE
                                    INSERT INTO [Place]
                                    (
                                        [Name],
                                        [Type],
                                        [Indoor],
                                        [Latitude],
                                        [Longitude],
                                        [Capacity],
                                        [Tag],
                                        [Active]
                                    )
                                    VALUES
                                    (
                                        @Name,
                                        @Type,
                                        @Indoor,
                                        @Latitude,
                                        @Longitude,
                                        @Capacity,
                                        @Tag,
                                        1
                                    );";

                var parameters = new DynamicParameters();
                parameters.Add("@Name", place.Name, DbType.String);
                parameters.Add("@Type", place.Type, DbType.String);
                parameters.Add("@Indoor", place.Indoor, DbType.Boolean);
                parameters.Add("@Latitude", place.Latitude, DbType.Decimal);
                parameters.Add("@Longitude", place.Longitude, DbType.Decimal);
                parameters.Add("@Capacity", place.Capacity, DbType.Int32);
                parameters.Add("@Tag", place.Tag, DbType.String);

                var affectedRows = _connection.Execute(sql, parameters);
                return affectedRows > 0 ? place : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating place.");
                return null;
            }
        }

        public async Task<Place?> UpdateAsync(Place place, CancellationToken ct = default)
        {
            const string sql = @"
                            UPDATE dbo.Place
                               SET [Name]      = @Name,
                                   [Type]      = @Type,
                                   [Indoor]    = @Indoor,
                                   [Latitude]  = @Latitude,
                                   [Longitude] = @Longitude,
                                   [Capacity]  = @Capacity,
                                   [Tag]       = @Tag
                             WHERE Id = @Id AND Active = 1;

                            SELECT TOP 1 *
                            FROM dbo.Place
                            WHERE Id = @Id AND Active = 1;";

            var parameters = new DynamicParameters();
            parameters.Add("@Id", place.Id, DbType.Int32);
            parameters.Add("@Name", place.Name, DbType.String);
            parameters.Add("@Type", place.Type, DbType.String);
            parameters.Add("@Indoor", place.Indoor, DbType.Boolean);
            parameters.Add("@Latitude", place.Latitude, DbType.Decimal);
            parameters.Add("@Longitude", place.Longitude, DbType.Decimal);
            parameters.Add("@Capacity", place.Capacity, DbType.Int32);
            parameters.Add("@Tag", place.Tag, DbType.String);

            try
            {
                return await _connection.QueryFirstOrDefaultAsync<Place>(
                    new CommandDefinition(sql, parameters, cancellationToken: ct));
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                throw new InvalidOperationException($"Place name '{place.Name}' already exists.", ex);
            }
        }
    }
}


































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.