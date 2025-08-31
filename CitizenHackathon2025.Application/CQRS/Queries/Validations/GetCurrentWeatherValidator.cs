using CitizenHackathon2025.Application.Abstractions.Validations;
using CitizenHackathon2025.Domain.LocalBusinessRules.Validations;

namespace CitizenHackathon2025.Application.CQRS.Queries.Validations
{
    public class GetCurrentWeatherValidator : ILocalValidator<GetCurrentWeatherQuery>
    {
        public Task ValidateAsync(GetCurrentWeatherQuery request, CancellationToken cancellationToken)
        {
            if (!WeatherForecastValidator.IsCityNameValid(request.City))
                throw new ArgumentException($"Le nom de ville '{request.City}' est invalide.");

            return Task.CompletedTask;
        }
    }
}
