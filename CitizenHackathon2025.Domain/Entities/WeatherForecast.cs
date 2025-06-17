namespace Citizenhackathon2025.Domain.Entities
{
    public class WeatherForecast
    {
        public int Id { get; set; }
        public DateTime DateWeather { get; set; }
        public int TemperatureC { get; set; }
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
        public string? Summary { get; set; }
        public double RainfallMm { get; set; }
        public int Humidity { get; set; }
        public double WindSpeedKmh { get; set; }
    }
    // DEPRECATED: Migrer to WeatherForecast DDD
    //[Obsolete("Use the DDD-based WeatherForecast entity instead.")]
    //public class LegacyWeatherForecast { ... }
}
















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.