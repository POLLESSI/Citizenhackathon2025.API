using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CitizenHackathon2025.Infrastructure.NoSql.Mongo.Collections
{
    public sealed class WeatherSnapshotDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public int? WeatherForecastId { get; set; }

        public int? PlaceId { get; set; }

        public int? EventId { get; set; }

        public string? PlaceName { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double? TemperatureC { get; set; }

        public double? FeelsLikeC { get; set; }

        public double? WindSpeedKmh { get; set; }

        public double? RainfallMm { get; set; }

        public double? HumidityPercent { get; set; }

        public string? WeatherType { get; set; }

        public string? Severity { get; set; }

        public string? Provider { get; set; }

        public string? Summary { get; set; }

        public string? Description { get; set; }

        public bool IsSevere { get; set; }

        public bool IsCritical { get; set; }

        public DateTime ForecastAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.