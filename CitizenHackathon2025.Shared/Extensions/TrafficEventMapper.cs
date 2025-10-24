using CitizenHackathon2025.Domain.Enums;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Shared.Extensions
{
    public static class TrafficEventMapper
    {
        public static TrafficEventDTO Map(RawTrafficEvent raw)
        {
            // 1) Normalize level (enum Domain -> int DTO)
            // raw.Level is INT (according to error CS8121), we are NOT testing "is string" here.
            TrafficLevel levelEnum = Enum.IsDefined(typeof(TrafficLevel), raw.Level)
                ? (TrafficLevel)raw.Level
                : TrafficLevel.FreeFlow;
            int level = (int)levelEnum; // the DTO expects an int

            // 2) Normalize Id (DTO = int). raw.Id is STRING → TryParse, otherwise 0.
            int id = 0;
            if (!string.IsNullOrWhiteSpace(raw.Id) && int.TryParse(raw.Id, out var parsedId))
            {
                id = parsedId;
            }

            // 3) Map the correct property names of the DTO
            return new TrafficEventDTO
            {
                Id = id,
                Latitude = raw.Latitude,
                Longitude = raw.Longitude,
                Level = level,
                Description = raw.Description ?? "No description",
                Timestamp = raw.Timestamp
            }
;
        }

        public static IEnumerable<TrafficEventDTO> MapAll(IEnumerable<RawTrafficEvent> raws)
            => raws.Select(Map);
    }
}

































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.