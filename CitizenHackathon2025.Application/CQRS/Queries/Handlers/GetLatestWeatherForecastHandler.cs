using MediatR;
using Citizenhackathon2025.Application.CQRS.Queries;
using static CitizenHackathon2025.Application.Extensions.MapperExtensions;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Application.CQRS.Queries.Handlers
{
    public class GetLatestWeatherForecastHandler : IRequestHandler<GetLatestWeatherForecastQuery, WeatherForecastDTO?>
    {
        private readonly IWeatherForecastRepository _repository;

        public GetLatestWeatherForecastHandler(IWeatherForecastRepository repository)
        {
            _repository = repository;
        }

        public async Task<WeatherForecastDTO?> Handle(GetLatestWeatherForecastQuery request, CancellationToken cancellationToken)
        {
            var model = await _repository.GetLatestWeatherForecastAsync();
            if (model == null) return null;

            return model.MapToWeatherForecastDTO();
        }
    }
}






































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.