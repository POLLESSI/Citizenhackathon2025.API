namespace CitizenHackathon2025.Infrastructure.ReadRows
{
    public sealed class WeatherForecastReadRow
    {
        public long Id { get; set; }
        public DateTime DateWeather { get; set; }   // DB column
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int TemperatureC { get; set; }
        public int TemperatureF { get; set; }
        public string Summary { get; set; } = "";
        public double? RainfallMm { get; set; }
        public int? Humidity { get; set; }
        public double? WindSpeedKmh { get; set; }
        public bool Active { get; set; }
        public string? WeatherMain { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? IconUrl { get; set; }
        public int WeatherType { get; set; }     // or WeatherType enum if you prefer
        public bool IsSevere { get; set; }
    }
}


























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.