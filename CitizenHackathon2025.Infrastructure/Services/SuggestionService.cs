using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Dapper;
using CitizenHackathon2025.Infrastructure.Extensions;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class SuggestionService : ISuggestionService
    {
    #nullable disable
        private readonly ISuggestionRepository _suggestionRepository;
        private readonly IPlaceRepository _placeRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IMistralAIService _mistralService;
        private readonly IUserRepository _userRepository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SuggestionService> _logger;

        public SuggestionService(ISuggestionRepository suggestionRepository, IPlaceRepository placeRepository, IEventRepository eventRepository, IMistralAIService mistralService, IUserRepository userRepository, IMemoryCache cache, ILogger<SuggestionService> logger)
        {
            _suggestionRepository = suggestionRepository;
            _placeRepository = placeRepository;
            _eventRepository = eventRepository;
            _mistralService = mistralService;
            _userRepository = userRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IEnumerable<Suggestion?>> GetLatestSuggestionAsync(CancellationToken ct = default)
            => await _suggestionRepository.GetLatestSuggestionAsync();
        public async Task<IEnumerable<Suggestion>> GetAllSuggestionsAsync(string prompt, int limit = 100, CancellationToken ct = default)
        {
            var cacheKey = $"Mistral_{prompt.Hash()}";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Suggestion> cachedResponse))
                return cachedResponse;

            var response = await _mistralService.GetAllSuggestionsAsync(prompt, ct);
            _cache.Set(cacheKey, response, TimeSpan.FromHours(1));
            return response;
        }
        public async Task<Suggestion?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var suggestion = await _suggestionRepository.GetByIdAsync(id);
                if (suggestion == null || !suggestion.Active) return null;
                return suggestion;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving suggestion by ID {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<Suggestion> SaveSuggestionAsync(Suggestion suggestion, CancellationToken ct = default)
            => await _suggestionRepository.SaveSuggestionAsync(suggestion);

        public async Task<IEnumerable<Suggestion>> GenerateSuggestionAsync(string context, CancellationToken ct)
        {
            var prompt = $"Generates a suggestion for a user in this context : {context}. " +
                         "Complies with GDPR regulations and does not store any personal data.";
            //var anonymizedPrompt = $"A user (ID: {userId.Hash()}) looking for suggestions for {location}. " +
            //"Offers alternatives without using personal data.";
            
            return await _mistralService.GetAllSuggestionsAsync(prompt, ct);
        }

        public async Task<Suggestion?> GenerateAndSaveSuggestionAsync(
            string context,
            int userId,
            string? originalPlace = null,
            string? suggestedAlternatives = null,
            string? reason = null,
            int? eventId = null,
            int? placeId = null,
            int? forecastId = null,
            int? trafficId = null,
            string? locationName = null,
            double? latitude = null,
            double? longitude = null,
            double? distanceKm = null,
            string? locationLabel = null,
            string? title = null,
            CancellationToken ct = default)
        {
            try
            {
                var prompt = $"Generates a suggestion for a user (ID: {userId}) in this context: {context}. Respecte le RGPD.";
                var suggestionText = await _mistralService.GenerateSuggestionAsync(prompt, ct);

                var suggestion = new Suggestion
                {
                    User_Id = userId,
                    Message = suggestionText,
                    Context = context,
                    DateSuggestion = DateTime.UtcNow,
                    Active = true,
                    OriginalPlace = originalPlace,
                    SuggestedAlternatives = suggestedAlternatives,
                    Reason = reason,
                    EventId = eventId,
                    PlaceId = placeId,
                    ForecastId = forecastId,
                    TrafficId = trafficId,
                    LocationName = locationName,
                    Latitude = latitude,
                    Longitude = longitude,
                    DistanceKm = distanceKm,
                    LocationLabel = locationLabel,
                    Title = title
                };

                return await _suggestionRepository.SaveSuggestionAsync(suggestion, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating and saving suggestion for user {UserId}.", userId);
                return null;
            }
        }
        public async Task<IEnumerable<Suggestion>> GetSuggestionsByUserAsync(int userId, CancellationToken ct = default)
            => await _suggestionRepository.GetSuggestionsByUserAsync(userId);

        public async Task<bool> SoftDeleteSuggestionAsync(int id, CancellationToken ct = default)
            => await _suggestionRepository.SoftDeleteSuggestionAsync(id);

        public Suggestion? UpdateSuggestion(Suggestion suggestion)
            => _suggestionRepository.UpdateSuggestion(suggestion);

        public async Task<IReadOnlyList<SuggestionGroupedByPlaceDTO>> GroupSuggestionsByPlaceAsync(DateTime? since = null, CancellationToken ct = default)
        {
            since ??= DateTime.UtcNow.AddDays(-7);

            // 1) Suggestion pool
            var raw = await _suggestionRepository.GetAllSuggestionsAsync(limit: 500, ct);
            var suggestions = raw
                .Where(s => s is not null)
                .Select(s => s!) // non-null
                .Where(s => s.Active && s.DateSuggestion >= since.Value)
                .ToList();

            // 2) Geographic Index Place + Event
            var places = (await _placeRepository.GetLatestPlaceAsync(limit: 500, ct))
                .Where(p => p is not null)
                .Select(p => p!)
                .ToList();

            var events = (await _eventRepository.GetLatestEventAsync(limit: 500, ct))
                .Where(e => e is not null)
                .Select(e => e!)
                .ToList();

            var geoIndex = new Dictionary<string, (string Type, bool Indoor, decimal Lat, decimal Lon)>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var p in places)
            {
                geoIndex[p.Name] = (
                    Type: p.Type,
                    Indoor: p.Indoor,
                    Lat: p.Latitude,
                    Lon: p.Longitude
                );
            }

            foreach (var ev in events)
            {
                geoIndex[ev.Name] = (
                    Type: "Event",
                    Indoor: !ev.IsOutdoor,
                    Lat: ev.Latitude,
                    Lon: ev.Longitude
                );
            }

            // 3) Aggregation
            var map = new Dictionary<string, SuggestionGroupedByPlaceDTO>(StringComparer.OrdinalIgnoreCase);

            foreach (var s in suggestions)
            {
                var names = new List<string>();

                if (!string.IsNullOrWhiteSpace(s.OriginalPlace))
                    names.Add(s.OriginalPlace.Trim());

                if (!string.IsNullOrWhiteSpace(s.SuggestedAlternatives))
                {
                    var parts = s.SuggestedAlternatives
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim());
                    names.AddRange(parts);
                }

                foreach (var name in names.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!geoIndex.TryGetValue(name, out var geo))
                        continue;

                    if (!map.TryGetValue(name, out var dto))
                    {
                        dto = new SuggestionGroupedByPlaceDTO
                        {
                            PlaceName = name,
                            Type = geo.Type,
                            Indoor = geo.Indoor,
                            Latitude = (double)geo.Lat,
                            Longitude = (double)geo.Lon,
                            CrowdLevel = "Unknown",
                            SuggestionCount = 0,
                            LastSuggestedAt = DateTime.MinValue,
                            Suggestions = new()
                        };
                        map[name] = dto;
                    }

                    dto.SuggestionCount++;
                    if (s.DateSuggestion > dto.LastSuggestedAt)
                        dto.LastSuggestedAt = s.DateSuggestion;

                    // 🔁 Domain → DTO
                    dto.Suggestions!.Add(s.MapToSuggestionDTO());
                }
            }

            return map.Values
                      .OrderByDescending(x => x.SuggestionCount)
                      .ThenByDescending(x => x.LastSuggestedAt)
                      .ToList();
        }
        public async Task<bool> DeleteUserDataAsync(int userId, CancellationToken ct)
        {
            // Removing user suggestions
            var suggestions = await _suggestionRepository.GetSuggestionsByUserAsync(userId);
            foreach (var suggestion in suggestions)
            {
                await _suggestionRepository.SoftDeleteSuggestionAsync(suggestion.Id);
            }

            // User anonymization
            await _userRepository.AnonymizeUserAsync(userId, ct);
            return true;
        }

        public bool IsPromptCompliant(string prompt)
        {
            var forbiddenPatterns = new[] { "email:", "phone:", "address:" };
            return !forbiddenPatterns.Any(p => prompt.Contains(p, StringComparison.OrdinalIgnoreCase));
        }

    }
}

























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.