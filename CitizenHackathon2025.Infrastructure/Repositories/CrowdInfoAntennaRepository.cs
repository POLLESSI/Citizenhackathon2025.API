using System.Data;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public sealed class CrowdInfoAntennaRepository : ICrowdInfoAntennaRepository
    {
        private readonly IDbConnection _db;

        public CrowdInfoAntennaRepository(IDbConnection db) => _db = db;

        public async Task<IReadOnlyList<CrowdInfoAntenna>> GetAllAsync(CancellationToken ct)
        {
            const string sql = @"
                            SELECT Id, Name, Latitude, Longitude, Active, CreatedUtc, Description
                            FROM dbo.CrowdInfoAntenna
                            WHERE Active = 1
                            ORDER BY Id DESC;";

            var rows = await _db.QueryAsync<CrowdInfoAntenna>(new CommandDefinition(sql, cancellationToken: ct));
            return rows.AsList();
        }

        public async Task<CrowdInfoAntenna?> GetByIdAsync(int id, CancellationToken ct)
        {
            const string sql = @"
                            SELECT TOP(1) Id, Name, Latitude, Longitude, Active, CreatedUtc, Description
                            FROM dbo.CrowdInfoAntenna
                            WHERE Id = @Id AND Active = 1;";

            return await _db.QueryFirstOrDefaultAsync<CrowdInfoAntenna>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        }

        public async Task<(CrowdInfoAntenna Antenna, double DistanceMeters)?> GetNearestAsync(
            double lat, double lng, double maxRadiusMeters, CancellationToken ct)
        {
            const string sql = @"
                            DECLARE @p geography = geography::Point(@Lat, @Lng, 4326);

                            SELECT TOP(1)
                                a.Id, a.Name, a.Latitude, a.Longitude, a.Active, a.CreatedUtc, a.Description,
                                @p.STDistance(a.GeoLocation) AS DistanceMeters
                            FROM dbo.CrowdInfoAntenna a
                            WHERE a.Active = 1
                              AND a.GeoLocation.STDistance(@p) <= @MaxRadiusMeters
                            ORDER BY a.GeoLocation.STDistance(@p) ASC;";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Lat", lat);
            parameters.Add("Lng", lng);
            parameters.Add("MaxRadiusMeters", maxRadiusMeters);

            var row = await _db.QueryFirstOrDefaultAsync<dynamic>(new CommandDefinition(sql, parameters, cancellationToken: ct));

            if (row is null) return null;

            var antenna = new CrowdInfoAntenna
            {
                Id = (int)row.Id,
                Name = (string?)row.Name ?? "",
                Latitude = (double)row.Latitude,
                Longitude = (double)row.Longitude,
                Active = (bool)row.Active,
                CreatedUtc = (DateTime)row.CreatedUtc,
                Description = (string?)row.Description
            };

            double dist = (double)row.DistanceMeters;
            return (antenna, dist);
        }
    }
}








































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.