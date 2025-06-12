using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Shared.DTOs;
using System.Globalization;

namespace CitizenHackathon2025.Shared.Extentions
{
    public static class WeatherForecastDTOExtensions
    {
        public static WeatherForecast? ToNumeric(this WeatherForecastDTO dto)
        {
            if (dto == null) return null;

            try
            {
                return new WeatherForecast
                {
                    Id = dto.Id,
                    DateWeather = dto.DateWeather,
                    TemperatureC = int.TryParse(dto.TemperatureC, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tempC) ? tempC : 0,
                    RainfallMm = double.TryParse(dto.RainfallMm, NumberStyles.Float, CultureInfo.InvariantCulture, out var rain) ? rain : 0.0,
                    Humidity = int.TryParse(dto.Humidity, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hum) ? hum : 0,
                    WindSpeedKmh = double.TryParse(dto.WindSpeedKmh, NumberStyles.Float, CultureInfo.InvariantCulture, out var wind) ? wind : 0.0,
                    Summary = dto.Summary ?? ""
                };
            }
            catch
            {
                // Tu peux aussi logguer ici ou lancer une exception custom si tu préfères
                return null;
            }
        }
    }
}
