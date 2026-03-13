namespace CitizenHackathon2025.Domain.DTOs
{
    public sealed class LocalAiWeatherContextDTO
    {
        public int Id { get; set; }

        public DateTime? DateWeather { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public int? TemperatureC { get; set; }
        public int? TemperatureF { get; set; }

        public string? Summary { get; set; }
        public double? RainfallMm { get; set; }
        public int? Humidity { get; set; }
        public double? WindSpeedKmh { get; set; }

        public string? WeatherMain { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? IconUrl { get; set; }

        public int? WeatherType { get; set; }
        public bool? IsSevere { get; set; }

        public double? DistanceKm { get; set; }
        public bool Active { get; set; }
    }
}