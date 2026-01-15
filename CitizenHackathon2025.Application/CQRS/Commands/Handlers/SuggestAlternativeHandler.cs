using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Interfaces.OpenWeather;
using CitizenHackathon2025.DTOs.UI;
using MediatR;

namespace CitizenHackathon2025.Application.CQRS.Commands.Handlers
{
    public class SuggestAlternativeCommandHandler : IRequestHandler<SuggestAlternativeCommand, SuggestionUIResponseDTO>
    {
        #nullable disable
        private readonly ICrowdInfoService _crowd;
        private readonly ITrafficConditionService _traffic;
        private readonly IOpenWeatherService _weather;
        private readonly IGPTService _ai;

        public SuggestAlternativeCommandHandler(ICrowdInfoService crowd, ITrafficConditionService traffic, IOpenWeatherService weather, IGPTService ai)
        {
            _crowd = crowd;
            _traffic = traffic;
            _weather = weather;
            _ai = ai;
        }

        public async Task<SuggestionUIResponseDTO> Handle(SuggestAlternativeCommand request, CancellationToken ct)
        {
            var crowd = await _crowd.GetCrowdLevelAsync(request.Destination);
            var traffic = await _traffic.CheckRoadAsync(request.UserPosition, request.Destination);
            var weather = await _weather.GetForecastAsync(request.Destination);

            if (weather == null)
            {
                return new SuggestionUIResponseDTO
                {
                    Text = "Unable to retrieve weather data.",
                    Severity = "Medium",
                    Icon = "❓"
                };
            }

            bool isSevereWeather = weather.Summary.Contains("rain") || weather.Summary.Contains("thunderstorm");

            if (isSevereWeather || traffic.IsBlocked || crowd.IsOverloaded)
            {
                var reason = $"{weather.Summary}, {traffic.Description}, high attendance.";

                var alt = await _ai.GenerateSuggestionAsync(
                    $"Give me an alternative to {request.Destination} in Belgium because of {reason}.");

                return new SuggestionUIResponseDTO
                {
                    Text = alt,
                    Severity = "High",
                    Icon = "⚠️"
                };
            }

            return new SuggestionUIResponseDTO
            {
                Text = "The destination is recommended.",
                Severity = "Low",
                Icon = "✅"
            };
        }
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.