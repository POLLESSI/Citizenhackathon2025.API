using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CitizenHackathon2025.Infrastructure.NoSql.Mongo.Collections
{
    public sealed class CrowdSnapshotDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public int? CrowdInfoId { get; set; }

        public int? PlaceId { get; set; }

        public int? EventId { get; set; }

        public string? PlaceName { get; set; }

        public string? EventName { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public int? CurrentCount { get; set; }

        public int? Capacity { get; set; }

        public double? DensityRatio { get; set; }

        public string? CrowdLevel { get; set; }

        public string? Source { get; set; }

        public string? Message { get; set; }

        public bool IsCritical { get; set; }

        public DateTime SnapshotAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}






























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.