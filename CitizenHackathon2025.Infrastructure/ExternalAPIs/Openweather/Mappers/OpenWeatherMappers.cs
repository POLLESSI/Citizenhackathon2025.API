using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Models;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.OpenWeather;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Mappers
{
    public static class OpenWeatherMappers
    {
        public static WeatherAlertEntity MapAlert(OneCallAlert a, decimal lat, decimal lon, DateTime nowUtc)
        {
            var start = DateTimeOffset.FromUnixTimeSeconds(a.Start).UtcDateTime;
            var end = DateTimeOffset.FromUnixTimeSeconds(a.End).UtcDateTime;

            var externalId = $"{a.SenderName}|{a.Event}|{a.Start}|{a.End}";

            return new WeatherAlertEntity
            {
                Provider = "openweather",
                ExternalId = externalId,
                Latitude = lat,
                Longitude = lon,
                SenderName = a.SenderName,
                EventName = a.Event,
                Description = a.Description,
                Tags = a.Tags is { Count: > 0 } ? string.Join(",", a.Tags) : null,
                Severity = GuessSeverity(a),
                LastSeenAt = nowUtc,
                Active = true
            };
        }

        public static WeatherForecast MapCurrentToForecast(OneCallResponse one, decimal lat, decimal lon)
        {
            var cur = one.Current;
            if (cur is null)
                throw new InvalidOperationException("OneCall current is null");

            var dt = DateTimeOffset.FromUnixTimeSeconds(cur.Dt).UtcDateTime;
            var weather = cur.Weather?.FirstOrDefault();

            var windKmh = cur.WindSpeed * 3.6;
            var rain1h = cur.Rain is not null && cur.Rain.TryGetValue("1h", out var v) ? v : 0.0;

            return new WeatherForecast
            {
                DateWeatherUtc = dt,
                Latitude = lat,
                Longitude = lon,
                TemperatureC = (int)Math.Round(cur.Temp),
                Humidity = cur.Humidity,
                WindSpeedKmh = Math.Round(windKmh, 1),
                RainfallMm = rain1h,
                Summary = weather?.Description ?? weather?.Main ?? "current",
                WeatherMain = weather?.Main ?? "",
                Description = weather?.Description,
                Icon = weather?.Icon,
                IconUrl = !string.IsNullOrWhiteSpace(weather?.Icon)
                    ? $"https://openweathermap.org/img/wn/{weather.Icon}@2x.png"
                    : "",
                WeatherType = MapWeatherType(weather?.Main, weather?.Description),
                IsSevere = GuessWeatherSeverity(weather?.Main, weather?.Description)
            };
        }

        public static WeatherForecast MapCurrent25ToForecast(OpenWeatherResponse cur)
        {
            if (cur is null)
                throw new ArgumentNullException(nameof(cur));

            var dt = DateTimeOffset.FromUnixTimeSeconds(cur.dt).UtcDateTime;
            var windKmh = (cur.wind?.speed ?? 0) * 3.6;

            var weather = cur.weather?.FirstOrDefault();
            var main = weather?.main;
            var desc = weather?.description ?? "current";
            var icon = weather?.icon ?? "";

            return new WeatherForecast
            {
                DateWeatherUtc = dt,
                Latitude = cur.coord.lat,
                Longitude = cur.coord.lon,
                TemperatureC = (int)Math.Round(cur.main?.temp ?? 0),
                Humidity = cur.main?.humidity ?? 0,
                WindSpeedKmh = Math.Round(windKmh, 1),
                RainfallMm = 0.0,
                Summary = desc,
                WeatherMain = main ?? "",
                Description = desc,
                Icon = icon,
                IconUrl = !string.IsNullOrWhiteSpace(icon)
                    ? $"https://openweathermap.org/img/wn/{icon}@2x.png"
                    : "",
                WeatherType = MapWeatherType(main, desc),
                IsSevere = GuessWeatherSeverity(main, desc)
            };
        }

        private static WeatherType MapWeatherType(string? weatherMain, string? description)
        {
            var main = weatherMain?.ToLowerInvariant() ?? "";
            var desc = description?.ToLowerInvariant() ?? "";

            return main switch
            {
                "clear" => WeatherType.Clear,

                "clouds" => desc switch
                {
                    var d when d.Contains("few") => WeatherType.PartlyCloudy,
                    var d when d.Contains("scattered") => WeatherType.PartlyCloudy,
                    var d when d.Contains("broken") => WeatherType.Cloudy,
                    var d when d.Contains("overcast") => WeatherType.Overcast,
                    _ => WeatherType.Cloudy
                },

                "rain" => WeatherType.Rain,
                "drizzle" => WeatherType.Drizzle,
                "thunderstorm" => WeatherType.Thunderstorm,
                "snow" => WeatherType.Snow,
                "mist" => WeatherType.Mist,
                "fog" => WeatherType.Fog,
                "smoke" => WeatherType.Smoke,
                "ash" => WeatherType.VolcanicAsh,

                _ => WeatherType.Unknown
            };
        }

        private static bool GuessWeatherSeverity(string? weatherMain, string? description)
        {
            var main = weatherMain?.ToLowerInvariant() ?? "";
            var desc = description?.ToLowerInvariant() ?? "";

            if (main is "thunderstorm")
                return true;

            if (desc.Contains("violent") || desc.Contains("heavy") || desc.Contains("extreme"))
                return true;

            return false;
        }

        private static byte? GuessSeverity(OneCallAlert a)
        {
            var ev = (a.Event ?? "").ToLowerInvariant();

            if (ev.Contains("red") || ev.Contains("danger"))
                return 4;

            if (ev.Contains("orange") || ev.Contains("warning"))
                return 3;

            return 2;
        }
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.