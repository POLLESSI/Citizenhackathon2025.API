using System.Data;
using Dapper;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class EventReadService : IEventReadService
    {
        private readonly IDbConnection _db;

        public EventReadService(IDbConnection db) => _db = db;

        public async Task<(double Latitude, double Longitude)?> GetEventGeoAsync(int eventId, CancellationToken ct)
        {
            // Variant A: Event directly a Latitude/Longitude
            const string sqlA = @"
                            SELECT TOP(1) Latitude, Longitude
                            FROM dbo.[Event]
                            WHERE Id = @EventId AND Active = 1
                              AND Latitude IS NOT NULL AND Longitude IS NOT NULL;";

            var direct = await _db.QueryFirstOrDefaultAsync<(double Latitude, double Longitude)>(
                new CommandDefinition(sqlA, new { EventId = eventId }, cancellationToken: ct));

            if (direct != default) return direct;

            // Variant B: Event -> PlaceId -> Place.Latitude/Longitude
            const string sqlB = @"
                            SELECT TOP(1) p.Latitude, p.Longitude
                            FROM dbo.[Event] e
                            JOIN dbo.[Place] p ON p.Id = e.PlaceId
                            WHERE e.Id = @EventId
                              AND e.Active = 1
                              AND p.Active = 1;";

            var viaPlace = await _db.QueryFirstOrDefaultAsync<(double Latitude, double Longitude)>(
                new CommandDefinition(sqlB, new { EventId = eventId }, cancellationToken: ct));

            return viaPlace == default ? null : viaPlace;
        }
    }
}






















































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.