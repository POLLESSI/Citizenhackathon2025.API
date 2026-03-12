using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using System.Text.Json;

namespace CitizenHackathon2025.Application.Services
{
    public class MistralContextBuilder
    {
        private readonly ICrowdInfoRepository _crowdInfoRepo;
        private readonly IEventRepository _eventRepo;
        private readonly IPlaceRepository _placeRepo;
        private readonly ITrafficConditionRepository _trafficRepo;
        private readonly IWeatherForecastRepository _weatherRepo;
        private readonly ISuggestionRepository _suggestionRepo;

        public MistralContextBuilder(
            ICrowdInfoRepository crowdInfoRepo,
            IEventRepository eventRepo,
            IPlaceRepository placeRepo,
            ITrafficConditionRepository trafficRepo,
            IWeatherForecastRepository weatherRepo,
            ISuggestionRepository suggestionRepo)
        {
            _crowdInfoRepo = crowdInfoRepo;
            _eventRepo = eventRepo;
            _placeRepo = placeRepo;
            _trafficRepo = trafficRepo;
            _weatherRepo = weatherRepo;
            _suggestionRepo = suggestionRepo;
        }

        public async Task<string> BuildContextAsync(string userPrompt, double? latitude = null, double? longitude = null,int radiusKm = 50)
        {
            // 1. Retrieve the relevant data in parallel
            var tasks = new List<Task>
            {
                _crowdInfoRepo.GetNearbyCrowdInfoAsync(latitude, longitude, radiusKm, CancellationToken.None),
                _eventRepo.GetUpcomingEventsAsync(latitude, longitude, radiusKm, CancellationToken.None),
                _placeRepo.GetNearbyPlacesAsync(latitude, longitude, radiusKm, CancellationToken.None),
                _suggestionRepo.GetRecentSuggestionsAsync(latitude, longitude, radiusKm, CancellationToken.None),
                _trafficRepo.GetRecentTrafficConditionsAsync(latitude, longitude, radiusKm, CancellationToken.None),
                _weatherRepo.GetCurrentWeatherAsync(latitude, longitude, CancellationToken.None)
            };

            await Task.WhenAll(tasks);

            var crowdInfo = ((Task<IEnumerable<CrowdInfo>>)tasks[0]).Result;
            var events = ((Task<IEnumerable<Event>>)tasks[1]).Result;
            var places = ((Task<IEnumerable<Place>>)tasks[2]).Result;
            var suggestions = ((Task<IEnumerable<Suggestion>>)tasks[3]).Result;
            var traffic = ((Task<IEnumerable<TrafficCondition>>)tasks[4]).Result;
            var weather = ((Task<WeatherForecast>)tasks[5]).Result;

            // 2. Building the structured context for Mistral
            return $@"
                ### Current Tourism Context (Radius: {radiusKm}km around lat={latitude}, lon={longitude})

                #### Upcoming events:
                {(events.Any() ? string.Join("\n- ", events.Select(e =>
                   $"{e.Name} ({e.DateEvent:yyyy-MM-dd}) to {e.Latitude},{e.Longitude} (Crowd level expected: {e.ExpectedCrowd})")) : "No major events.")}

                #### Nearby tourist places:
                {(places.Any() ? string.Join("\n- ", places.Select(p =>
                   $"{p.Name} ({p.Type}, Capacity: {p.Capacity})")) : "No tourist places nearby.")}

                #### Traffic conditions:
                {(traffic.Any() ? string.Join("\n- ", traffic.Select(t =>
                   $"{t.IncidentType} à {t.Latitude},{t.Longitude} (Level: {t.CongestionLevel})")) : "Smooth traffic.")}

                #### Current weather:
                {weather?.Summary ?? "Not available"} (Temperature: {weather?.TemperatureC}°C, Wind: {weather?.WindSpeedKmh} km/h, Rain: {weather?.RainfallMm} mm)

                #### Real-time crowd level:
                {(crowdInfo.Any() ? string.Join("\n- ", crowdInfo.Select(c =>
                   $"{c.LocationName}: Level {c.CrowdLevel} (Last update: {c.Timestamp:HH:mm})")) : "No recent crowd data.")}

                #### Recent suggestions:
                {(suggestions.Any() ? string.Join("\n- ", suggestions.Select(s =>
                   $"{s.OriginalPlace} → {s.SuggestedAlternatives} (Reason: {s.Reason})")) : "No recent suggestions.")}

                ---
                ### User question:
                {userPrompt}

                ---
                ### Instructions for Mistral:
                1. Analyze the context to provide a precise answer.
                2. Offers alternatives if the venue is overcrowded.
                3. Format your answer in Markdown with clear headings.
                ";
        }

    }
}

























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.