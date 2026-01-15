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

            // ExternalId stable: sender|event|start|end (sufficient, OneCall does not have a standard unique ID)
            var externalId = $"{a.SenderName}|{a.Event}|{a.Start}|{a.End}";

            return new WeatherAlertEntity
            {
                Provider = "openweather",
                ExternalId = externalId,
                Latitude = lat,
                Longitude = lon,
                SenderName = a.SenderName,
                EventName = a.Event,
                StartUtc = start,
                EndUtc = end,
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
            if (cur is null) throw new InvalidOperationException("OneCall current is null");

            var dt = DateTimeOffset.FromUnixTimeSeconds(cur.Dt).UtcDateTime;
            var weather = cur.Weather?.FirstOrDefault();

            // wind_speed is m/s -> km/h
            var windKmh = cur.WindSpeed * 3.6;
            var rain1h = cur.Rain is not null && cur.Rain.TryGetValue("1h", out var v) ? v : 0.0;

            return new WeatherForecast
            {
                DateWeather = dt,
                Latitude = lat,
                Longitude = lon,
                TemperatureC = (int)Math.Round(cur.Temp),
                Humidity = cur.Humidity,
                WindSpeedKmh = Math.Round(windKmh, 1),
                RainfallMm = rain1h,
                Summary = weather?.Description ?? weather?.Main ?? "current"
            };
        }

        public static WeatherForecast MapCurrent25ToForecast(OpenWeatherResponse cur)
        {
            // dt = unix seconds
            var dt = DateTimeOffset.FromUnixTimeSeconds(cur.dt).UtcDateTime;

            // wind.speed is m/s -> km/h
            var windKmh = (cur.wind?.speed ?? 0) * 3.6;

            var desc = cur.weather?.FirstOrDefault()?.description ?? "current";

            return new WeatherForecast
            {
                DateWeather = dt,
                Latitude = cur.coord.lat,
                Longitude = cur.coord.lon,
                TemperatureC = (int)Math.Round(cur.main?.temp ?? 0),
                Humidity = cur.main?.humidity ?? 0,
                WindSpeedKmh = Math.Round(windKmh, 1),
                RainfallMm = 0.0,            // /data/2.5/weather does not always return rain(1h) depending on the payload
                Summary = desc
            };
        }

        private static byte? GuessSeverity(OneCallAlert a)
        {
            // Minimal heuristic; you can refine it (tags, keywords, duration, etc.)
            var ev = (a.Event ?? "").ToLowerInvariant();
            if (ev.Contains("red") || ev.Contains("danger")) return 4;
            if (ev.Contains("orange") || ev.Contains("warning")) return 3;
            return 2; // default moderate
        }
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.