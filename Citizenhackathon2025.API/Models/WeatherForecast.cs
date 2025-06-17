namespace Citizenhackathon2025.API.Models
{
    public class WeatherForecast
    {
    #nullable disable
        public DateTime Date { get; set; }
        public int TemperatureC { get; set; }
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
        public string Summary { get; set; }
        public string RainfallMm { get; set; }
        public string Humidity { get; set; }
        public string WindSpeedKmh { get; set; }
    }
}
















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.