using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using MediatR;

namespace CitizenHackathon2025.Application.CQRS.Queries.Handlers
{
    public class GetWeatherHistoryHandler : IRequestHandler<GetWeatherHistoryQuery, List<WeatherForecastDTO>>
    {
        private readonly IWeatherForecastRepository _repository;

        public GetWeatherHistoryHandler(IWeatherForecastRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<WeatherForecastDTO>> Handle(GetWeatherHistoryQuery request, CancellationToken cancellationToken)
        {
            var history = await _repository.GetHistoryAsync(request.Limit);
            return history.Select(h => h.MapToWeatherForecastDTO()).ToList();
        }
    }
}














































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.