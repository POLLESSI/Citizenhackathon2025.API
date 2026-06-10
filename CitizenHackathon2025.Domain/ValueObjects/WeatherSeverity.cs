using System;
using CitizenHackathon2025.Contracts.Enums;

namespace CitizenHackathon2025.Domain.ValueObjects
{
    /// <summary>
    /// Represents the overall severity of weather conditions.
    /// Combines weather types with severity thresholds to assess risk.
    /// </summary>
    public sealed class WeatherSeverity : IEquatable<WeatherSeverity>
    {
        public WeatherType Type { get; }
        public SeverityLevel Level { get; }

        public WeatherSeverity(WeatherType type, SeverityLevel level)
        {
            Type = type;
            Level = level;
        }

        public static WeatherSeverity FromMetrics(
            WeatherType type,
            double temperature,
            double windSpeed,
            double rainMm)
        {
            if (type is WeatherType.Thunderstorm
                or WeatherType.Blizzard
                or WeatherType.Storm
                or WeatherType.Hail
                or WeatherType.Heatwave
                or WeatherType.ColdWave
                or WeatherType.BlackIce
                or WeatherType.FreezingRain)
            {
                return new WeatherSeverity(type, SeverityLevel.Critical);
            }

            if (rainMm > 20 || windSpeed > 60 || temperature < -5 || temperature > 35)
                return new WeatherSeverity(type, SeverityLevel.Moderate);

            return new WeatherSeverity(type, SeverityLevel.Mild);
        }

        public bool IsCritical => Level == SeverityLevel.Critical;

        public override string ToString() => $"{Type} ({Level})";

        public bool Equals(WeatherSeverity? other)
            => other is not null && Type == other.Type && Level == other.Level;

        public override bool Equals(object? obj)
            => obj is WeatherSeverity ws && Equals(ws);

        public override int GetHashCode()
            => HashCode.Combine(Type, Level);
    }
}





























































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.