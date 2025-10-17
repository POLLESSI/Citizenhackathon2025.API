using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Application.CQRS.Queries.Handlers
{
    public class GetWeatherHistoryQueryHandler : IRequestHandler<GetWeatherHistoryQuery, List<WeatherForecastDTO>>
    {
        private readonly IWeatherForecastRepository _repo;
        private readonly ILogger<GetWeatherHistoryQueryHandler> _logger;

        public GetWeatherHistoryQueryHandler(IWeatherForecastRepository repo, ILogger<GetWeatherHistoryQueryHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<List<WeatherForecastDTO>> Handle(GetWeatherHistoryQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repo.GetHistoryAsync();
            _logger.LogInformation($"Fetched {entities.Count()} entries from history.");

            return entities.Select(e => e.MapToWeatherForecastDTO()).ToList();
        }
    }
}























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.