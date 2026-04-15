using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using CitizenHackathon2025.Infrastructure.Extensions;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class SuggestionService : ISuggestionService
    {
#nullable disable
        private readonly ISuggestionRepository _suggestionRepository;
        private readonly IPlaceRepository _placeRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IMistralAIService _mistralService;
        private readonly IUserRepository _userRepository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SuggestionService> _logger;

        public SuggestionService(
            ISuggestionRepository suggestionRepository,
            IPlaceRepository placeRepository,
            IEventRepository eventRepository,
            IMistralAIService mistralService,
            IUserRepository userRepository,
            IMemoryCache cache,
            ILogger<SuggestionService> logger)
        {
            _suggestionRepository = suggestionRepository;
            _placeRepository = placeRepository;
            _eventRepository = eventRepository;
            _mistralService = mistralService;
            _userRepository = userRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IEnumerable<Suggestion>> GetLatestSuggestionAsync(CancellationToken ct = default)
        {
            var result = await _suggestionRepository.GetLatestSuggestionAsync();
            return result ?? Enumerable.Empty<Suggestion>();
        }

        public async Task<IEnumerable<Suggestion>> GetAllSuggestionsAsync(
            string prompt,
            int limit = 100,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return Enumerable.Empty<Suggestion>();

            var cacheKey = $"mistral:suggestions:{prompt.Hash()}:{limit}";

            if (_cache.TryGetValue(cacheKey, out IEnumerable<Suggestion> cachedResponse) &&
                cachedResponse is not null)
            {
                return cachedResponse;
            }

            var generatedText = await _mistralService.GenerateFromPromptAsync(prompt, ct);

            var response = new List<Suggestion>
            {
                new Suggestion
                {
                    Message = generatedText,
                    Context = prompt,
                    DateSuggestion = DateTime.UtcNow,
                    Active = true
                }
            };

            _cache.Set(cacheKey, response, TimeSpan.FromHours(1));
            return response;
        }

        public async Task<Suggestion?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var suggestion = await _suggestionRepository.GetByIdAsync(id);
                if (suggestion == null || !suggestion.Active)
                    return null;

                return suggestion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suggestion by id {Id}.", id);
                return null;
            }
        }

        public async Task<Suggestion> SaveSuggestionAsync(Suggestion suggestion, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(suggestion);
            return await _suggestionRepository.SaveSuggestionAsync(suggestion, ct);
        }

        public async Task<IEnumerable<Suggestion>> GenerateSuggestionAsync(string context, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(context))
                return Enumerable.Empty<Suggestion>();

            var prompt =
                $"Generate one useful suggestion for the following context: {context}. " +
                $"Respect GDPR and do not store or infer personal data.";

            var generatedText = await _mistralService.GenerateFromPromptAsync(prompt, ct);

            return new List<Suggestion>
            {
                new Suggestion
                {
                    Message = generatedText,
                    Context = context,
                    DateSuggestion = DateTime.UtcNow,
                    Active = true
                }
            };
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
                if (string.IsNullOrWhiteSpace(context))
                {
                    _logger.LogWarning("GenerateAndSaveSuggestionAsync called with empty context for user {UserId}.", userId);
                    return null;
                }

                var prompt =
                    $"Generate one useful local suggestion in French for the following context: {context}. " +
                    $"Respect GDPR and do not expose personal data.";

                var suggestionText = await _mistralService.GenerateFromPromptAsync(prompt, ct);

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
        {
            var result = await _suggestionRepository.GetSuggestionsByUserAsync(userId);
            return result ?? Enumerable.Empty<Suggestion>();
        }

        public async Task<bool> SoftDeleteSuggestionAsync(int id, CancellationToken ct = default)
        {
            return await _suggestionRepository.SoftDeleteSuggestionAsync(id);
        }

        public Suggestion? UpdateSuggestion(Suggestion suggestion)
        {
            ArgumentNullException.ThrowIfNull(suggestion);
            return _suggestionRepository.UpdateSuggestion(suggestion);
        }

        public async Task<IReadOnlyList<SuggestionGroupedByPlaceDTO>> GroupSuggestionsByPlaceAsync(
            DateTime? since = null,
            CancellationToken ct = default)
        {
            since ??= DateTime.UtcNow.AddDays(-7);

            var rawSuggestions = await _suggestionRepository.GetAllSuggestionsAsync(limit: 500, ct);
            var suggestions = rawSuggestions?
                .Where(s => s is not null)
                .Select(s => s!)
                .Where(s => s.Active && s.DateSuggestion >= since.Value)
                .ToList()
                ?? new List<Suggestion>();

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

            foreach (var place in places)
            {
                if (string.IsNullOrWhiteSpace(place.Name))
                    continue;

                geoIndex[place.Name] = (
                    Type: place.Type ?? "Unknown",
                    Indoor: place.Indoor,
                    Lat: place.Latitude,
                    Lon: place.Longitude
                );
            }

            foreach (var ev in events)
            {
                if (string.IsNullOrWhiteSpace(ev.Name))
                    continue;

                geoIndex[ev.Name] = (
                    Type: "Event",
                    Indoor: !ev.IsOutdoor,
                    Lat: ev.Latitude,
                    Lon: ev.Longitude
                );
            }

            var map = new Dictionary<string, SuggestionGroupedByPlaceDTO>(StringComparer.OrdinalIgnoreCase);

            foreach (var suggestion in suggestions)
            {
                var names = new List<string>();

                if (!string.IsNullOrWhiteSpace(suggestion.OriginalPlace))
                    names.Add(suggestion.OriginalPlace.Trim());

                if (!string.IsNullOrWhiteSpace(suggestion.SuggestedAlternatives))
                {
                    var parts = suggestion.SuggestedAlternatives
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
                            Suggestions = new List<SuggestionDTO>()
                        };

                        map[name] = dto;
                    }

                    dto.SuggestionCount++;

                    if (suggestion.DateSuggestion > dto.LastSuggestedAt)
                        dto.LastSuggestedAt = suggestion.DateSuggestion;

                    dto.Suggestions!.Add(suggestion.MapToSuggestionDTO());
                }
            }

            return map.Values
                .OrderByDescending(x => x.SuggestionCount)
                .ThenByDescending(x => x.LastSuggestedAt)
                .ToList();
        }

        public async Task<bool> DeleteUserDataAsync(int userId, CancellationToken ct)
        {
            var suggestions = await _suggestionRepository.GetSuggestionsByUserAsync(userId);

            foreach (var suggestion in suggestions)
            {
                await _suggestionRepository.SoftDeleteSuggestionAsync(suggestion.Id);
            }

            await _userRepository.AnonymizeUserAsync(userId, ct);
            return true;
        }

        public bool IsPromptCompliant(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return false;

            var forbiddenPatterns = new[] { "email:", "phone:", "address:" };
            return !forbiddenPatterns.Any(p => prompt.Contains(p, StringComparison.OrdinalIgnoreCase));
        }
    }
}

























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.