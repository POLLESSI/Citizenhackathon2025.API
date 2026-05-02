using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using System.Data;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public sealed class CrowdInfoAntennaRepository : ICrowdInfoAntennaRepository
    {
        private readonly IDbConnection _db;

        public CrowdInfoAntennaRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<CrowdInfoAntenna> CreateAntennaAsync(CrowdInfoAntenna antenna, CancellationToken ct)
        {
            const string sql = @"
                        INSERT INTO dbo.CrowdInfoAntenna
                            (Name, Latitude, Longitude, Active, Description, MaxCapacity)
                        OUTPUT
                            inserted.Id,
                            inserted.Name,
                            inserted.Latitude,
                            inserted.Longitude,
                            inserted.Active,
                            inserted.CreatedUtc,
                            inserted.Description,
                            inserted.MaxCapacity
                        VALUES
                            (@Name, @Latitude, @Longitude, 1, @Description, @MaxCapacity);";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Name", string.IsNullOrWhiteSpace(antenna.Name) ? null : antenna.Name.Trim());
            parameters.Add("Latitude", antenna.Latitude);
            parameters.Add("Longitude", antenna.Longitude);
            parameters.Add("Description", string.IsNullOrWhiteSpace(antenna.Description) ? null : antenna.Description.Trim());
            parameters.Add("MaxCapacity", antenna.MaxCapacity);

            var created = await _db.QuerySingleAsync<CrowdInfoAntenna>(
                new CommandDefinition(sql, parameters, cancellationToken: ct));

            return created;
        }

        public async Task<bool> DeleteAntennaAsync(int id, CancellationToken ct)
        {
            const string sql = @"
                        DELETE FROM dbo.CrowdInfoAntenna
                        WHERE Id = @Id
                          AND Active = 1;";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Id", id);

            var affected = await _db.ExecuteAsync(
                new CommandDefinition(sql, parameters, cancellationToken: ct));

            return affected > 0;
        }

        public async Task<IReadOnlyList<CrowdInfoAntenna>> GetActiveAsync(CancellationToken ct = default)
        {
            const string sql = """
                SELECT
                    Id,
                    ISNULL(Name, '') AS Name,
                    Latitude,
                    Longitude,
                    CAST(ISNULL(Active, 0) AS bit) AS Active,
                    CreatedUtc,
                    Description,
                    MaxCapacity
                FROM dbo.CrowdInfoAntenna
                WHERE Active = 1
                ORDER BY Id;
                """;

            var rows = await _db.QueryAsync<CrowdInfoAntenna>(
                new CommandDefinition(sql, cancellationToken: ct));

            return rows.AsList();
        }

        public async Task<IReadOnlyList<CrowdInfoAntenna>> GetByBoundsAsync(
            double minLat,
            double maxLat,
            double minLng,
            double maxLng,
            CancellationToken ct = default)
        {
            const string sql = """
                            SELECT
                                Id,
                                ISNULL(Name, '') AS Name,
                                Latitude,
                                Longitude,
                                CAST(ISNULL(Active, 0) AS bit) AS Active,
                                CreatedUtc,
                                Description,
                                MaxCapacity
                            FROM dbo.CrowdInfoAntenna
                            WHERE Active = 1
                              AND Latitude BETWEEN @MinLat AND @MaxLat
                              AND Longitude BETWEEN @MinLng AND @MaxLng
                            ORDER BY Id;
                            """;

            var rows = await _db.QueryAsync<CrowdInfoAntenna>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        MinLat = minLat,
                        MaxLat = maxLat,
                        MinLng = minLng,
                        MaxLng = maxLng
                    },
                    cancellationToken: ct));

            return rows.AsList();
        }

        public async Task<IReadOnlyList<CrowdInfoAntenna>> GetAllAsync(CancellationToken ct)
        {
            const string sql = @"
                            SELECT Id, Name, Latitude, Longitude, Active, CreatedUtc, Description, MaxCapacity
                            FROM dbo.CrowdInfoAntenna
                            WHERE Active = 1
                            ORDER BY Id DESC;";

            var rows = await _db.QueryAsync<CrowdInfoAntenna>(new CommandDefinition(sql, cancellationToken: ct));
            return rows.AsList();
        }

        public async Task<CrowdInfoAntenna?> GetByIdAsync(int id, CancellationToken ct)
        {
            const string sql = @"
                            SELECT TOP(1) Id, Name, Latitude, Longitude, Active, CreatedUtc, Description, MaxCapacity
                            FROM dbo.CrowdInfoAntenna
                            WHERE Id = @Id AND Active = 1;";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Id", id);

            return await _db.QueryFirstOrDefaultAsync<CrowdInfoAntenna>(
                new CommandDefinition(sql, parameters));
        }

        public async Task<(CrowdInfoAntenna Antenna, double DistanceMeters)?> GetNearestAsync(
            double lat,
            double lng,
            double maxRadiusMeters,
            CancellationToken ct)
        {
            const string sql = @"
                            DECLARE @p geography = geography::Point(@Lat, @Lng, 4326);

                            SELECT TOP(1)
                                a.Id,
                                a.Name,
                                a.Latitude,
                                a.Longitude,
                                a.Active,
                                a.CreatedUtc,
                                a.Description,
                                a.MaxCapacity,
                                @p.STDistance(a.GeoLocation) AS DistanceMeters
                            FROM dbo.CrowdInfoAntenna a
                            WHERE a.Active = 1
                              AND a.GeoLocation.STDistance(@p) <= @MaxRadiusMeters
                            ORDER BY a.GeoLocation.STDistance(@p) ASC;";

            var parameters = new DynamicParameters();
            parameters.Add("Lat", lat);
            parameters.Add("Lng", lng);
            parameters.Add("MaxRadiusMeters", maxRadiusMeters);

            var row = await _db.QueryFirstOrDefaultAsync<CrowdInfoAntennaNearestRow>(
                new CommandDefinition(sql, parameters, cancellationToken: ct));

            if (row is null)
                return null;

            var antenna = ToEntity(row);

            return (antenna, row.DistanceMeters);
        }
        private static CrowdInfoAntenna ToEntity(CrowdInfoAntennaNearestRow row)
            => new()
            {
                Id = row.Id,
                Name = row.Name ?? string.Empty,
                Latitude = (double)row.Latitude,
                Longitude = (double)row.Longitude,
                Active = row.Active,
                CreatedUtc = row.CreatedUtc,
                Description = row.Description,
                MaxCapacity = row.MaxCapacity
            };

        public async Task<CrowdInfoAntenna> UpsertFromCadastreAsync(CrowdInfoAntenna antenna, CancellationToken ct)
        {
            const string sql = "dbo.sp_CrowdInfoAntenna_UpsertFromCadastre";

            var parameters = new DynamicParameters();
            parameters.Add("@ExternalSource", antenna.ExternalSource);
            parameters.Add("@ExternalId", antenna.ExternalId);
            parameters.Add("@Name", antenna.Name);
            parameters.Add("@Latitude", Math.Round((decimal)antenna.Latitude, 6));
            parameters.Add("@Longitude", Math.Round((decimal)antenna.Longitude, 6));
            parameters.Add("@Description", antenna.Description);
            parameters.Add("@MaxCapacity", antenna.MaxCapacity);
            parameters.Add("@Active", antenna.Active);

            return await _db.QuerySingleAsync<CrowdInfoAntenna>(
                new CommandDefinition(
                    sql,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: ct));
        }

        internal sealed class CrowdInfoAntennaNearestRow
        {
            public int Id { get; set; }
            public string? Name { get; set; }

            public decimal Latitude { get; set; }
            public decimal Longitude { get; set; }

            public bool Active { get; set; }
            public DateTime CreatedUtc { get; set; }
            public string? Description { get; set; }
            public int? MaxCapacity { get; set; }

            public double DistanceMeters { get; set; }
        }
    }
}






















































































































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.