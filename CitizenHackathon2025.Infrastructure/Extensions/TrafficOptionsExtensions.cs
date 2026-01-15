using CitizenHackathon2025.Domain.ValueObjects;
using CitizenHackathon2025.Infrastructure.Options;

namespace CitizenHackathon2025.Infrastructure.Extensions
{
    public static class TrafficOptionsExtensions
    {
        public static BoundingBox ToBoundingBox(this TrafficOptions.TrafficBBoxOptions bbox)
            => new(bbox.MinLat, bbox.MinLon, bbox.MaxLat, bbox.MaxLon);
    }
}





































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.