using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using Dapper;
using System.Data;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class MessageCorrelationService : IMessageCorrelationService
    {
        private readonly IDbConnection _db;

        public MessageCorrelationService(IDbConnection db)
        {
            _db = db;
        }

        public async Task<UserMessage> CorrelateAsync(UserMessage raw, CancellationToken ct = default)
        {
            var tokens = ExtractTokens(raw.Content);

            // CrowdInfo
            foreach (var t in tokens)
            {
                var crowd = await _db.QueryFirstOrDefaultAsync<(int Id, string LocationName, decimal Lat, decimal Lon)>(
                    new CommandDefinition(@"
                SELECT TOP 1 Id, LocationName, Latitude, Longitude
                FROM dbo.CrowdInfo
                WHERE Active = 1
                  AND LocationName LIKE '%' + @t + '%'
                ORDER BY [Timestamp] DESC;",
                        new { t }, cancellationToken: ct));

                if (crowd.Id != 0)
                {
                    raw.SourceType = "Crowd";
                    raw.SourceId = crowd.Id;
                    raw.RelatedName = crowd.LocationName;
                    raw.Latitude = crowd.Lat;
                    raw.Longitude = crowd.Lon;
                    return raw;
                }
            }

            // Event
            foreach (var t in tokens)
            {
                var ev = await _db.QueryFirstOrDefaultAsync<(int Id, string Name, decimal Lat, decimal Lon)>(
                    new CommandDefinition(@"
                SELECT TOP 1 Id, [Name], Latitude, Longitude
                FROM dbo.Event
                WHERE Active = 1
                  AND [Name] LIKE '%' + @t + '%'
                ORDER BY DateEvent DESC;",
                        new { t }, cancellationToken: ct));

                if (ev.Id != 0)
                {
                    raw.SourceType = "Event";
                    raw.SourceId = ev.Id;
                    raw.RelatedName = ev.Name;
                    raw.Latitude = ev.Lat;
                    raw.Longitude = ev.Lon;
                    return raw;
                }
            }

            // Place
            foreach (var t in tokens)
            {
                var place = await _db.QueryFirstOrDefaultAsync<(int Id, string Name, decimal Lat, decimal Lon)>(
                    new CommandDefinition(@"
                SELECT TOP 1 Id, [Name], Latitude, Longitude
                FROM dbo.Place
                WHERE Active = 1
                  AND [Name] LIKE '%' + @t + '%'
                ORDER BY Id DESC;",
                        new { t }, cancellationToken: ct));

                if (place.Id != 0)
                {
                    raw.SourceType = "Place";
                    raw.SourceId = place.Id;
                    raw.RelatedName = place.Name;
                    raw.Latitude = place.Lat;
                    raw.Longitude = place.Lon;
                    return raw;
                }
            }

            raw.SourceType = "Other";
            return raw;
        }


        private static List<string> ExtractTokens(string content)
        {
            var sep = new[] { ' ', '\r', '\n', '\t', ',', ';', '.', '!', '?', ':', '"' };
            return content
                .Split(sep, StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.Length >= 3)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.