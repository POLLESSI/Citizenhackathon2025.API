using System;
using System.Text.RegularExpressions;

namespace CitizenHackathon2025.Application.Common
{
    public static class Validators
    {
        /// <summary>
        /// Checks if an email address is valid.
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Checks if a latitude is in the valid range [-90, 90].
        /// </summary>
        public static bool IsValidLatitude(decimal latitude)
        {
            return latitude >= -90 && latitude <= 90;
        }

        /// <summary>
        /// Checks if a longitude is in the valid range [-180, 180].
        /// </summary>
        public static bool IsValidLongitude(decimal longitude)
        {
            return longitude >= -180 && longitude <= 180;
        }

        /// <summary>
        /// Checks if a string contains only alphanumeric characters or spaces.
        /// </summary>
        public static bool IsAlphaNumeric(string input)
        {
            return Regex.IsMatch(input ?? "", @"^[a-zA-Z0-9\s]*$");
        }

        /// <summary>
        /// Checks if a date is future or current.
        /// </summary>
        public static bool IsFutureOrToday(DateTime date)
        {
            return date.Date >= DateTime.UtcNow.Date;
        }

        /// <summary>
        /// Checks if a password is strong.
        /// </summary>
        public static bool IsStrongPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;

            return Regex.IsMatch(password, @"[A-Z]") &&
                   Regex.IsMatch(password, @"[a-z]") &&
                   Regex.IsMatch(password, @"[0-9]") &&
                   Regex.IsMatch(password, @"[\W_]");
        }
    }
}





































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.