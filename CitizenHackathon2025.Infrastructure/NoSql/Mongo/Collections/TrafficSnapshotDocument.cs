using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CitizenHackathon2025.Infrastructure.NoSql.Mongo.Collections
{
    public sealed class TrafficSnapshotDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public int? TrafficConditionId { get; set; }

        public int? EventId { get; set; }

        public int? PlaceId { get; set; }

        public string? Source { get; set; }

        public string? RoadName { get; set; }

        public string? FromLocation { get; set; }

        public string? ToLocation { get; set; }

        public string? Severity { get; set; }

        public string? Status { get; set; }

        public string? Description { get; set; }

        public double? DelayMinutes { get; set; }

        public double? DistanceKm { get; set; }

        public double? AverageSpeedKmh { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public bool IsBlocking { get; set; }

        public bool IsCritical { get; set; }

        public DateTime? ObservedAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}














































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.