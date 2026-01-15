using System.Text.Json.Serialization;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Models
{
    public sealed class OneCallCurrent
    {
        // Unix seconds
        [JsonPropertyName("dt")] public long Dt { get; set; }

        // °C si units=metric
        [JsonPropertyName("temp")] public double Temp { get; set; }

        // %
        [JsonPropertyName("humidity")] public int Humidity { get; set; }

        // m/s (OpenWeather), to be converted to km/h if needed
        [JsonPropertyName("wind_speed")] public double WindSpeed { get; set; }

        // List “weather”: description, id, main, icon
        [JsonPropertyName("weather")] public List<OneCallWeather>? Weather { get; set; }

        // Rain volume for last 1 hour, mm
        [JsonPropertyName("rain")] public Dictionary<string, double>? Rain { get; set; }
    }
}




































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.