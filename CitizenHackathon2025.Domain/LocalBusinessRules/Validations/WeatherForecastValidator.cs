using System.Text.RegularExpressions;

namespace CitizenHackathon2025.Domain.LocalBusinessRules.Validations
{
    public static class WeatherForecastValidator
    {
        /// <summary>
        /// Check that the city name is well formed (no special characters prohibited).
        /// </summary>
        public static bool IsCityNameValid(string? city)
        {
            if (string.IsNullOrWhiteSpace(city)) return false;

            var trimmed = city.Trim();
            var regex = new Regex(@"^[a-zA-ZÀ-ÿ\-\s']{2,100}$"); // allows accents, spaces, dashes
            return regex.IsMatch(trimmed);
        }

        /// <summary>
        /// Check that the temperature is a valid double within a normal range.
        /// </summary>
        public static bool IsTemperatureValid(double temperature) =>
            !double.IsNaN(temperature) && !double.IsInfinity(temperature);

        /// <summary>
        /// Check that the forecast date is a plausible date (e.g. not earlier than 1950).
        /// </summary>
        public static bool IsForecastDateValid(DateTime dateUtc) =>
            dateUtc.Year >= 1950 && dateUtc.Year <= DateTime.UtcNow.AddYears(10).Year;

        /// <summary>
        /// Checks that the entire weather forecast model is valid.
        /// </summary>
        public static bool IsValid(string? city, double temperature, DateTime dateUtc) =>
            IsCityNameValid(city) &&
            IsTemperatureValid(temperature) &&
            IsForecastDateValid(dateUtc);
    }
}
