using CitizenHackathon2025.Application.Abstractions.Validations;
using CitizenHackathon2025.Domain.LocalBusinessRules.Validations;

namespace CitizenHackathon2025.Application.CQRS.Queries.Validations
{
    public class GetCurrentWeatherValidator : ILocalValidator<GetCurrentWeatherQuery>
    {
        public Task ValidateAsync(GetCurrentWeatherQuery request, CancellationToken cancellationToken)
        {
            if (!WeatherForecastValidator.IsCityNameValid(request.City))
                throw new ArgumentException($"The city name '{request.City}' is invalid.");

            return Task.CompletedTask;
        }
    }
}





































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.