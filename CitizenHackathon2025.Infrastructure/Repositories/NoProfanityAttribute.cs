using System.ComponentModel.DataAnnotations;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public class NoProfanityAttribute : ValidationAttribute
    {
    #nullable disable
        private readonly string[] _bannedWords = new[] { "merde", "con", "fuck", "shit", "idiot" };
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is string str && _bannedWords.Any(b => str.Contains(b, StringComparison.OrdinalIgnoreCase)))
            {
                return new ValidationResult("The field contains prohibited words.");
            }

            return ValidationResult.Success;
        }
    }
}
