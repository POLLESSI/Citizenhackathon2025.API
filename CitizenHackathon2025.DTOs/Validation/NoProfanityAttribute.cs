using System.ComponentModel.DataAnnotations;

namespace CitizenHackathon2025.DTOs.Validation
{
    public sealed class NoProfanityAttribute : ValidationAttribute
    {
        private static readonly string[] BannedWords =
        {
            "merde", "con", "fuck", "shit", "idiot"
        };

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string str || string.IsNullOrWhiteSpace(str))
                return ValidationResult.Success;

            return BannedWords.Any(b => str.Contains(b, StringComparison.OrdinalIgnoreCase))
                ? new ValidationResult("The field contains prohibited words.")
                : ValidationResult.Success;
        }
    }
}































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.