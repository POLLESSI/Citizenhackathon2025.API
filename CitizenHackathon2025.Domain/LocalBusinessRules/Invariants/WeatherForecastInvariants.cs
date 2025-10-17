namespace CitizenHackathon2025.Domain.LocalBusinessRules.Invariants
{
    public static class WeatherForecastInvariants
    {
        /// <summary>
        /// Checks that a city name is valid.
        /// </summary>
        public static bool IsValidCity(string? city) =>
            !string.IsNullOrWhiteSpace(city) && city.Length >= 2 && city.Length <= 100;

        /// <summary>
        /// Check that the temperature is within a realistic range (in °C).
        /// </summary>
        public static bool IsRealisticTemperature(double temperature) =>
            temperature > -100 && temperature < 100; // extrêmes théoriques

        /// <summary>
        /// Check that the forecast date is >= today (not in the past).
        /// </summary>
        public static bool IsValidForecastDate(DateTime forecastDateUtc) =>
            forecastDateUtc.Date >= DateTime.UtcNow.Date;

        /// <summary>
        /// Check that the weather data is consistent.
        /// </summary>
        public static bool IsForecastConsistent(string? city, double temperature, DateTime forecastDateUtc) =>
            IsValidCity(city) &&
            IsRealisticTemperature(temperature) &&
            IsValidForecastDate(forecastDateUtc);
    }
}




























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.