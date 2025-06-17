using Citizenhackathon2025.Application.Extensions;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Shared.DTOs;
using CitizenHackathon2025.Application.CQRS.Queries;
using MediatR;

namespace Citizenhackathon2025.Application.CQRS.Queries.Handlers
{
    public class GetCurrentWeatherHandler : IRequestHandler<GetCurrentWeatherQuery, WeatherForecastDTO?>
    {
        private readonly IWeatherForecastRepository _repository;

        public GetCurrentWeatherHandler(IWeatherForecastRepository repository)
        {
            _repository = repository;
        }

        public async Task<WeatherForecastDTO?> Handle(GetCurrentWeatherQuery request, CancellationToken cancellationToken)
        {
            var model = await _repository.GetLatestWeatherForecastAsync();
            if (model == null) return null;

            return model.MapToWeatherForecastDTO();
        }
    }
}










































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.