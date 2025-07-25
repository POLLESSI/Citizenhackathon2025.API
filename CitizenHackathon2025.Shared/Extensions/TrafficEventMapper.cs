using CitizenHackathon2025.Domain.Enums;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Shared.Extentions
{
    public static class TrafficEventMapper
    {
        public static TrafficEventDTO Map(RawTrafficEvent raw)
        {
            var level = Enum.IsDefined(typeof(TrafficLevel), raw.Level)
                ? (TrafficLevel)raw.Level
                : TrafficLevel.Low; // default value in case of error

            return new TrafficEventDTO
            {
                Id = raw.Id ?? Guid.NewGuid().ToString(),
                Description = raw.Description ?? "No description",
                Latitude = raw.Latitude,
                Longitude = raw.Longitude,
                Timestamp = raw.Timestamp,
                Level = level
            };
        }

        public static IEnumerable<TrafficEventDTO> MapAll(IEnumerable<RawTrafficEvent> raws)
            => raws.Select(Map);
    }
}

































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.