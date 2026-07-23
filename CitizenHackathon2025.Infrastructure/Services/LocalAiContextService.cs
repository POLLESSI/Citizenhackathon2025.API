using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.DTOs;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class LocalAiContextService : ILocalAiContextService
    {
        private const double DefaultLatitude = 50.4114;
        private const double DefaultLongitude = 4.4445;

        private readonly ILocalAiDataRepository _localAiRepo;
        private readonly ILogger<LocalAiContextService> _logger;

        private readonly record struct LocalAiDateRange(DateTime From, DateTime ToExclusive)
        {
            public bool IsSingleDay => ToExclusive == From.AddDays(1);

            public DateTime LastIncludedDate => ToExclusive.AddDays(-1);
        }


        private static readonly LocalAiContextLimits Limits = new();

        public LocalAiContextService(ILocalAiDataRepository localAiRepo, ILogger<LocalAiContextService> logger)
        {
            _localAiRepo = localAiRepo ?? throw new ArgumentNullException(nameof(localAiRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<LocalAiContextDTO> BuildContextAsync(string prompt, double? latitude, double? longitude, CancellationToken ct = default)
        {
            _logger.LogWarning(
                "[LOCAL AI LIMITS] RadiusKm={RadiusKm}; MaxPlaces={MaxPlaces}; " +
                "MaxEvents={MaxEvents}; MaxWeather={MaxWeather}",
                Limits.RadiusKm,
                Limits.MaxPlaces,
                Limits.MaxEvents,
                Limits.MaxWeather);
            var safePrompt = prompt?.Trim() ?? string.Empty;
            var hasChildren =
                safePrompt.Contains("enfant", StringComparison.OrdinalIgnoreCase) ||
                safePrompt.Contains("enfants", StringComparison.OrdinalIgnoreCase) ||
                safePrompt.Contains("famille", StringComparison.OrdinalIgnoreCase) ||
                safePrompt.Contains("kids", StringComparison.OrdinalIgnoreCase) ||
                safePrompt.Contains("children", StringComparison.OrdinalIgnoreCase);
            var lat = NormalizeLatitude(latitude);
            var lng = NormalizeLongitude(longitude);
            string? locationLabel = null;
            var keywordPlaces = await _localAiRepo.SearchPlacesByKeywordsAsync(safePrompt, limit: 10, ct);
            var radiusKm = NormalizeRadiusKm(Limits.RadiusKm);
            var targetDateRange = ResolveTargetDateRange(safePrompt);
            var dateFrom = targetDateRange.From;
            var dateToExclusive = targetDateRange.ToExclusive;
            var intent = ResolveIntent(safePrompt);
            var hasResolvedCoordinates = latitude.HasValue && longitude.HasValue;
            var requestedPlace = keywordPlaces.FirstOrDefault();

            if (!hasResolvedCoordinates && requestedPlace is { } resolvedPlace)
            {
                lat = resolvedPlace.Latitude ?? lat;
                lng = resolvedPlace.Longitude ?? lng;
                locationLabel = resolvedPlace.Name;
            }
            else if (hasResolvedCoordinates)
            {
                _logger.LogInformation(
                    "[LOCAL AI GEO] Keeping coordinates resolved " +
                    "by the orchestrator. Lat={Lat}; Lng={Lng}",
                    lat,
                    lng);
            }

            foreach (var keywordPlace in keywordPlaces)
            {
                if (!keywordPlace.Latitude.HasValue || !keywordPlace.Longitude.HasValue)
                {
                    keywordPlace.DistanceKm = null;
                    continue;
                }

                keywordPlace.DistanceKm = HaversineKm(lat, lng, keywordPlace.Latitude.Value, keywordPlace.Longitude.Value);
            }

            _logger.LogInformation(
                "Building local AI context. " +
                "PromptLength={PromptLength}, " +
                "Lat={Lat}, Lng={Lng}, RadiusKm={RadiusKm}, " +
                "DateFrom={DateFrom:yyyy-MM-dd}, " +
                "DateToExclusive={DateToExclusive:yyyy-MM-dd}, " +
                "Intent={@Intent}",
                safePrompt.Length,
                lat,
                lng,
                radiusKm,
                dateFrom,
                dateToExclusive,
                intent);

            Task<IEnumerable<LocalAiPlaceContextDTO>> placesTask = intent.NeedPlaces
                ? _localAiRepo.GetNearbyPlacesAsync(lat, lng, radiusKm, ct)
                : Task.FromResult(Enumerable.Empty<LocalAiPlaceContextDTO>());

            Task<IEnumerable<LocalAiEventContextDTO>> eventsTask = intent.NeedEvents
                ? _localAiRepo.GetNearbyEventsAsync(lat, lng, dateFrom, dateToExclusive, radiusKm, ct)
                : Task.FromResult(Enumerable.Empty<LocalAiEventContextDTO>());

            Task<IEnumerable<LocalAiCrowdCalendarContextDTO>> crowdCalendarTask = intent.NeedCrowdCalendar
                ? _localAiRepo.GetNearbyCrowdCalendarAsync(lat, lng, dateFrom, dateToExclusive, radiusKm, ct)
                : Task.FromResult(Enumerable.Empty<LocalAiCrowdCalendarContextDTO>());

            Task<IEnumerable<LocalAiCrowdInfoContextDTO>> crowdInfoTask = intent.NeedCrowdInfo
                ? _localAiRepo.GetNearbyCrowdInfoAsync(lat, lng, dateFrom, radiusKm, ct)
                : Task.FromResult(Enumerable.Empty<LocalAiCrowdInfoContextDTO>());

            Task<IEnumerable<LocalAiTrafficContextDTO>> trafficTask = intent.NeedTraffic ? _localAiRepo.GetNearbyTrafficAsync(lat, lng, dateFrom, dateToExclusive, radiusKm, ct)
                : Task.FromResult(Enumerable.Empty<LocalAiTrafficContextDTO>());

            Task<IEnumerable<LocalAiWeatherContextDTO>> weatherTask = intent.NeedWeather ? _localAiRepo.GetNearbyWeatherAsync(lat, lng, dateFrom, dateToExclusive, radiusKm, ct)
                : Task.FromResult(Enumerable.Empty<LocalAiWeatherContextDTO>());

            Task<IEnumerable<LocalAiCriticalAlertContextDTO>> criticalAlertsTask =
                _localAiRepo.GetNearbyCriticalAlertsAsync(lat, lng, radiusKm, ct);

            await Task.WhenAll(
                placesTask,
                eventsTask,
                crowdCalendarTask,
                crowdInfoTask,
                trafficTask,
                weatherTask,
                criticalAlertsTask).ConfigureAwait(false);

            var events = (await eventsTask.ConfigureAwait(false))
                .Where(IsEventRelevant)
                .OrderBy(x => x.DistanceKm ?? double.MaxValue)
                .ThenByDescending(x => x.CrowdLevel ?? int.MinValue)
                .ThenBy(x => x.EventDate ?? DateTime.MaxValue)
                .Take(Limits.MaxEvents)
                .ToList();

            _logger.LogWarning("[LOCAL AI EVENTS] " + "DateFrom={DateFrom:yyyy-MM-dd}; " + "DateToExclusive={DateToExclusive:yyyy-MM-dd}; " + "Count={Count}", dateFrom, dateToExclusive, events.Count);

            foreach (var currentEvent in events)
            {
                _logger.LogWarning(
                    "[LOCAL AI EVENT] " +
                    "Id={Id}; Title={Title}; City={City}; " +
                    "Date={Date}; DistanceKm={DistanceKm}; " +
                    "MaxCapacity={MaxCapacity}",
                    currentEvent.Id,
                    currentEvent.Title,
                    currentEvent.City,
                    currentEvent.EventDate,
                    currentEvent.DistanceKm,
                    currentEvent.MaxCapacity);
            }

            var crowdCalendar = (await crowdCalendarTask.ConfigureAwait(false))
                .Where(IsCrowdCalendarRelevant)
                .OrderBy(x => x.DistanceKm ?? double.MaxValue)
                .ThenByDescending(x => x.ExpectedLevel ?? int.MinValue)
                .ThenByDescending(x => x.Confidence ?? int.MinValue)
                .Take(Limits.MaxCrowdCalendar)
                .ToList();

            var crowdInfo = (await crowdInfoTask.ConfigureAwait(false))
                .Where(IsCrowdInfoRelevant)
                .OrderByDescending(x => x.Timestamp ?? DateTime.MinValue)
                .ThenBy(x => x.DistanceKm ?? double.MaxValue)
                .Take(Limits.MaxCrowdInfo)
                .ToList();

            var traffic = (await trafficTask.ConfigureAwait(false))
                .Where(IsTrafficRelevant)
                .OrderByDescending(x => x.Severity ?? int.MinValue)
                .ThenBy(x => x.DistanceKm ?? double.MaxValue)
                .Take(Limits.MaxTraffic)
                .ToList();

            var weather = (await weatherTask.ConfigureAwait(false))
                .Where(IsWeatherSignificant)
                .OrderBy(x => x.DistanceKm ?? double.MaxValue)
                .ThenBy(x => x.DateWeather ?? DateTime.MaxValue)
                .Take(Limits.MaxWeather)
                .ToList();

            var badWeatherDetected = weather.Any(w =>
                w.IsSevere == true ||
                (w.RainfallMm ?? 0d) > 0d ||
                (w.WindSpeedKmh ?? 0d) >= 45d ||
                (w.TemperatureC ?? 15d) <= 0d ||
                (w.TemperatureC ?? 15d) >= 32d);

            var criticalAlerts = (await criticalAlertsTask.ConfigureAwait(false))
                .Where(a => a.Status == "Confirmed")
                .OrderByDescending(a => a.Severity)
                .ThenBy(a => a.DistanceKm ?? double.MaxValue)
                .Take(20)
                .ToList();

            var nearbyPlaces = (await placesTask.ConfigureAwait(false))
                .Where(IsPlaceRelevant)
                .Where(p => !IsUnsafeCandidate(p, criticalAlerts))
                .ToList();

            var mergedPlaces = keywordPlaces
                .Concat(nearbyPlaces)
                .GroupBy(p => p.Id)
                .Select(g => g.First());

            var places = DeduplicateNearbyPlaces(mergedPlaces, duplicateRadiusKm: 0.1)
                .OrderBy(p => p.DistanceKm ?? double.MaxValue)
                .ThenBy(p => p.Name)
                .Take(Limits.MaxPlaces)
                .ToList();

            if (badWeatherDetected)
            {
                places = places
                    .OrderByDescending(p => p.Indoor == true)
                    .ThenBy(p => p.DistanceKm ?? double.MaxValue)
                    .ToList();
            }

            if (hasChildren)
            {
                places = places
                    .OrderByDescending(p =>
                        (p.Tag ?? "").Contains("child", StringComparison.OrdinalIgnoreCase) ||
                        (p.Tag ?? "").Contains("famille", StringComparison.OrdinalIgnoreCase) ||
                        (p.Tag ?? "").Contains("enfant", StringComparison.OrdinalIgnoreCase))
                    .ThenBy(p => p.DistanceKm ?? double.MaxValue)
                    .ToList();
            }


            _logger.LogInformation("Local AI context built. Places={Places}, Events={Events}, CrowdCalendar={CrowdCalendar}, CrowdInfo={CrowdInfo}, Traffic={Traffic}, Weather={Weather}, CriticalAlerts={CriticalAlerts}",
                places.Count,
                events.Count,
                crowdCalendar.Count,
                crowdInfo.Count,
                traffic.Count,
                weather.Count,
                criticalAlerts.Count);

            return new LocalAiContextDTO
            {
                UserPrompt = safePrompt,
                Latitude = lat,
                Longitude = lng,
                // Compatibility with the old code :
                TargetDate = dateFrom,
                TargetDateFrom = dateFrom,
                TargetDateToExclusive = dateToExclusive,
                Places = places,
                Events = events,
                CrowdCalendar = crowdCalendar,
                CrowdInfo = crowdInfo,
                Traffic = traffic,
                Weather = weather,
                CriticalAlerts = criticalAlerts,
                LocationLabel = locationLabel,
                KeywordMatchedPlaces = DeduplicateNearbyPlaces(keywordPlaces, duplicateRadiusKm: 0.1).ToList(),
                HasChildren = hasChildren,
                BadWeatherDetected = badWeatherDetected,
                MaxAlternativeRadiusKm = 25
            };
        }

        public string BuildPrompt(LocalAiContextDTO context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var prioritizeCurrentEvents = context.Events.Count > 0 && HasDateSensitiveIntent(context.UserPrompt);

            var sb = new StringBuilder(4096);

            sb.AppendLine("You are OutZen local assistant.");
            sb.AppendLine("Answer only with facts present in the provided context.");
            sb.AppendLine("Do not invent places, visits, events, traffic, weather, or crowd data.");
            sb.AppendLine("Do not choose the response language here.");
            sb.AppendLine("The response language is controlled by the system message.");
            sb.AppendLine("Be concise, concrete, and useful.");
            sb.AppendLine();

            sb.AppendLine("Priority rules:");

            if (prioritizeCurrentEvents)
            {
                sb.AppendLine("- Current-date events remain the first priority.");
                sb.AppendLine("- Use explicitly matched places to locate those events and nearby attractions.");
            }
            else
            {
                sb.AppendLine("- If the user mentions a place by name, rely on the explicitly matched places.");
            }
            sb.AppendLine("5. Use towns and villages only as geographical context.");
            sb.AppendLine("6. Never invent missing information.");

            sb.AppendLine();

            sb.AppendLine("Critical safety rules:");
            
            sb.AppendLine();

            sb.AppendLine("Response style:");

            if (prioritizeCurrentEvents)
            {
                sb.AppendLine("- start with relevant events occurring within the requested date range");
                sb.AppendLine("- clearly state its name, locality, date and supplied distance");
                sb.AppendLine("- then add the most relevant permanent attractions");
                sb.AppendLine("- an event located at distance 0 km must not be omitted");
            }
            else
            {
                sb.AppendLine("- start with the most relevant concrete attraction");
                sb.AppendLine("- include relevant nearby events when available");
            }

            sb.AppendLine("- use generic towns only to locate attractions or events");
            sb.AppendLine("- keep the answer concise and factual");

            sb.AppendLine();

            sb.AppendLine($"Question: {context.UserPrompt}");
            if (context.TargetDateToExclusive == context.TargetDateFrom.AddDays(1))
            {
                sb.AppendLine($"Requested date: " + $"{context.TargetDateFrom:yyyy-MM-dd}");
            }
            else
            {
                var lastIncludedDate = context.TargetDateToExclusive.AddDays(-1);

                sb.AppendLine($"Requested date range: " + $"{context.TargetDateFrom:yyyy-MM-dd} " + $"through {lastIncludedDate:yyyy-MM-dd} inclusive.");
            }
            sb.AppendLine($"Coordinates: {context.Latitude:F6}, {context.Longitude:F6}");
            sb.AppendLine();

            AppendPlaces(sb, context);
            AppendEvents(sb, context);
            AppendCrowdCalendar(sb, context);
            AppendCrowdInfo(sb, context);
            AppendTraffic(sb, context);
            AppendWeather(sb, context);
            AppendCriticalAlerts(sb, context);
            AppendUserSafetyConstraints(sb, context);

            sb.AppendLine("Final reminder:");
            if (prioritizeCurrentEvents)
            {
                sb.AppendLine("- current-date events first");
                sb.AppendLine("- permanent attractions second");
            }
            else
            {
                sb.AppendLine("- concrete attractions first");
                sb.AppendLine("- relevant events second");
            }

            sb.AppendLine("- practical constraints third");
            sb.AppendLine("- no invention");
            sb.AppendLine("- no weather-as-attraction");

            sb.AppendLine("Distance rules:");
            sb.AppendLine("- Use only the distances explicitly written in the context.");
            sb.AppendLine("- Never estimate, infer, recalculate, or invent distances.");
            sb.AppendLine("- Copy distances exactly as written.");
            sb.AppendLine("- If distance is missing, write 'distance non disponible'.");
            sb.AppendLine("- Do not convert distances from km to meters.");
            sb.AppendLine("- Do not round distances differently.");
            sb.AppendLine("- Copy the distance string exactly, including unit.");
            sb.AppendLine("- If the context says 'distance 16.5 km', answer '16.5 km', not 'environ 16 km'.");
            sb.AppendLine();

            sb.AppendLine("Critical safety rules:");
            sb.AppendLine("1. If the requested or nearest destination is affected by a confirmed Crowd, Weather, Traffic, or Disaster alert, do not recommend it.");
            sb.AppendLine("2. Propose safer alternatives outside the affected zone.");
            sb.AppendLine("3. Safety has priority over distance.");
            sb.AppendLine("4. Alternatives may be up to 20-25 km away, including outside Wallonia if safer.");
            sb.AppendLine("5. If weather is rainy, stormy, windy, icy, snowy, or severe, prioritize indoor places.");
            sb.AppendLine("6. If children are present, avoid unsafe, isolated, overcrowded, road-exposed, or disaster-adjacent places.");
            sb.AppendLine("7. Never increase crowd concentration near a critical alert zone.");
            sb.AppendLine("8. Clearly explain why the original destination is not recommended.");
            sb.AppendLine("9. Do not route users toward an alert zone.");
            sb.AppendLine();

            var hasConfirmedAlert = context.CriticalAlerts is not null && context.CriticalAlerts.Count > 0;

            if (hasConfirmedAlert)
            {
                sb.AppendLine("Confirmed safety-alert rules:");
                sb.AppendLine("- Explain the confirmed alert using only supplied facts.");
                sb.AppendLine("- Do not recommend a place situated inside the affected area.");
                sb.AppendLine("- Prefer relevant alternatives outside the affected area.");
                sb.AppendLine();
            }

            if (context.HasChildren)
            {
                sb.AppendLine("Child-related factuality rules:");
                sb.AppendLine("- Do not say a place is supervised unless the context proves it.");
                sb.AppendLine("- Do not say a place is child-friendly unless its type or tag proves it.");
                sb.AppendLine("- If suitability for children is unknown, state it briefly.");
                sb.AppendLine("- Do not add a long generic warning about children.");
                sb.AppendLine();
            }

            sb.AppendLine("Tourism answer format:");

            if (prioritizeCurrentEvents)
            {
                sb.AppendLine("- Mention every relevant current-date event first.");
                sb.AppendLine("- Then add the most useful permanent attractions.");
                sb.AppendLine("- Recommend 3 to 5 items in total, including events.");
                sb.AppendLine("- An event occurring at the requested location during the requested period must be item 1.");
            }
            else
            {
                sb.AppendLine("- Recommend 3 to 5 actual attractions when available.");
                sb.AppendLine("- Include relevant real events when useful.");
            }

            sb.AppendLine("- Prefer concrete attractions over generic cities or villages.");
            sb.AppendLine("- Use cities and villages only to locate events or attractions.");
            sb.AppendLine("- Mention the exact supplied distance for every item.");
            sb.AppendLine("- Do not invent opening hours, facilities or attractions.");
            sb.AppendLine();

            sb.AppendLine("Places explicitly matched from user request:");
            sb.AppendLine("These places were found by backend keyword search in dbo.Place. They are factual database results.");

            foreach (var p in context.KeywordMatchedPlaces)
            {
                sb.AppendLine(
                    $"- name: {p.Name}; " +
                    $"type: {p.Type ?? "unknown"}; " +
                    $"indoor: {(p.Indoor == true ? "true" : p.Indoor == false ? "false" : "unknown")}; " +
                    $"lat: {p.Latitude}; lng: {p.Longitude}; " +
                    $"capacity: {(p.Capacity?.ToString() ?? "unknown")}; " +
                    $"tag: {p.Tag ?? "none"}");
            }

            sb.AppendLine();

            sb.AppendLine("Place search rules:");
            sb.AppendLine("- If the user mentions a place by name, first rely on 'Places explicitly matched from user request'.");
            sb.AppendLine("- Do not require the user to provide coordinates.");
            sb.AppendLine("- Do not invent coordinates, attractions, indoor status, child-friendly status, or distances.");
            sb.AppendLine("- If several places match the keyword, mention the most relevant matches and ask the user to clarify only if necessary.");
            sb.AppendLine("- If the user mentions a place by name, first rely on 'Places explicitly matched from user request'.");

            sb.AppendLine();

            return sb.ToString();
        }

        private static double NormalizeLatitude(double? latitude)
            => latitude ?? DefaultLatitude;

        private static double NormalizeLongitude(double? longitude)
            => longitude ?? DefaultLongitude;

        private static double NormalizeRadiusKm(double radiusKm)
            => radiusKm > 0d ? radiusKm : 25d;
        private static bool IsPlaceRelevant(LocalAiPlaceContextDTO p)
        {
            if (p is null) return false;
            if (string.IsNullOrWhiteSpace(p.Name)) return false;
            return true;
        }

        private static bool IsEventRelevant(LocalAiEventContextDTO e)
        {
            if (e is null) return false;
            if (string.IsNullOrWhiteSpace(e.Title) && string.IsNullOrWhiteSpace(e.City)) return false;
            return true;
        }

        private static bool IsCrowdCalendarRelevant(LocalAiCrowdCalendarContextDTO e)
        {
            if (e is null) return false;
            if (string.IsNullOrWhiteSpace(e.EventName) && string.IsNullOrWhiteSpace(e.RegionCode)) return false;
            return true;
        }

        private static bool IsCrowdInfoRelevant(LocalAiCrowdInfoContextDTO c)
        {
            if (c is null) return false;
            if (string.IsNullOrWhiteSpace(c.LocationName)) return false;
            return true;
        }

        private static bool IsTrafficRelevant(LocalAiTrafficContextDTO t)
        {
            if (t is null) return false;

            return !string.IsNullOrWhiteSpace(t.Title)
                || !string.IsNullOrWhiteSpace(t.IncidentType)
                || !string.IsNullOrWhiteSpace(t.Road)
                || (t.Severity ?? 0) > 0;
        }

        private static bool IsWeatherSignificant(LocalAiWeatherContextDTO w)
        {
            if (w is null) return false;

            if (w.IsSevere == true) return true;
            if ((w.RainfallMm ?? 0d) >= 5.0d) return true;
            if ((w.WindSpeedKmh ?? 0d) >= 50.0d) return true;
            if ((w.TemperatureC ?? 15d) <= 0d) return true;
            if ((w.TemperatureC ?? 15d) >= 32d) return true;

            var main = (w.WeatherMain ?? string.Empty).ToLowerInvariant();
            var desc = (w.Description ?? string.Empty).ToLowerInvariant();
            var summary = (w.Summary ?? string.Empty).ToLowerInvariant();

            if (main.Contains("storm") || main.Contains("snow") || main.Contains("thunder"))
                return true;

            if (desc.Contains("orage") || desc.Contains("neige") || desc.Contains("forte pluie"))
                return true;

            if (summary.Contains("storm") || summary.Contains("snow") || summary.Contains("thunder"))
                return true;

            return false;
        }

        private static bool IsUnsafeCandidate(LocalAiPlaceContextDTO place, IReadOnlyList<LocalAiCriticalAlertContextDTO> alerts)
        {
            if (place.Latitude is null || place.Longitude is null)
                return false;

            foreach (var alert in alerts)
            {
                var distanceKm = HaversineKm(
                    (double)place.Latitude.Value,
                    (double)place.Longitude.Value,
                    (double)alert.Latitude,
                    (double)alert.Longitude);

                var unsafeRadiusKm = alert.AlertKind switch
                {
                    "Disaster" => 5.0,
                    "Crowd" => 2.0,
                    "Traffic" => 2.0,
                    "Weather" => 3.0,
                    _ => 1.0
                };

                if (distanceKm <= unsafeRadiusKm)
                    return true;
            }

            return false;
        }

        private static bool HasDateSensitiveIntent(string? prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return false;

            var normalized =
                prompt.ToLowerInvariant();

            return
                normalized.Contains("aujourd") ||
                normalized.Contains("demain") ||
                normalized.Contains("cette semaine") ||
                normalized.Contains("dans la semaine") ||
                normalized.Contains("ce weekend") ||
                normalized.Contains("ce week-end") ||
                normalized.Contains("ce samedi") ||
                normalized.Contains("ce dimanche") ||
                normalized.Contains("ce soir");
        }

        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double r = 6371.0;

            static double Rad(double x) => x * Math.PI / 180.0;

            var dLat = Rad(lat2 - lat1);
            var dLon = Rad(lon2 - lon1);

            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return r * c;
        }

        private static IReadOnlyList<LocalAiPlaceContextDTO>
    DeduplicateNearbyPlaces(
        IEnumerable<LocalAiPlaceContextDTO> places,
        double duplicateRadiusKm = 0.1)
        {
            ArgumentNullException.ThrowIfNull(places);

            var result = new List<LocalAiPlaceContextDTO>();

            foreach (var place in places
                         .Where(IsPlaceRelevant)
                         .OrderBy(p => p.DistanceKm ?? double.MaxValue)
                         .ThenBy(p => p.Name))
            {
                // One cannot make a geographical comparison involving an incomplete location.
                // We are keeping it, but deduplication based on coordinates
                // cannot be applied to it.
                if (!place.Latitude.HasValue ||
                    !place.Longitude.HasValue)
                {
                    var sameNameAlreadyExists = result.Any(existing =>
                        string.Equals(
                            existing.Name?.Trim(),
                            place.Name?.Trim(),
                            StringComparison.OrdinalIgnoreCase));

                    if (!sameNameAlreadyExists)
                        result.Add(place);

                    continue;
                }

                var existingIndex = result.FindIndex(existing =>
                {
                    if (!existing.Latitude.HasValue ||
                        !existing.Longitude.HasValue)
                    {
                        return false;
                    }

                    var distanceKm = HaversineKm(
                        place.Latitude.Value,
                        place.Longitude.Value,
                        existing.Latitude.Value,
                        existing.Longitude.Value);

                    return distanceKm <= duplicateRadiusKm;
                });

                if (existingIndex < 0)
                {
                    result.Add(place);
                    continue;
                }

                var existingPlace = result[existingIndex];

                if (IsBetterCanonicalPlace(place, existingPlace))
                {
                    // Retain the shortest distance calculated by SQL.
                    place.DistanceKm = MinNullable(
                        place.DistanceKm,
                        existingPlace.DistanceKm);

                    result[existingIndex] = place;
                }
                else
                {
                    existingPlace.DistanceKm = MinNullable(
                        existingPlace.DistanceKm,
                        place.DistanceKm);
                }
            }

            return result;
        }

        private static bool IsBetterCanonicalPlace(
            LocalAiPlaceContextDTO candidate,
            LocalAiPlaceContextDTO current)
        {
            var candidateName = candidate.Name?.Trim() ?? string.Empty;
            var currentName = current.Name?.Trim() ?? string.Empty;

            var candidateHasHyphen = candidateName.Contains('-');
            var currentHasHyphen = currentName.Contains('-');

            if (candidateHasHyphen != currentHasHyphen)
                return candidateHasHyphen;

            var candidateLooksAbbreviated =
                candidateName.StartsWith("St ", StringComparison.OrdinalIgnoreCase) ||
                candidateName.StartsWith("St-", StringComparison.OrdinalIgnoreCase);

            var currentLooksAbbreviated =
                currentName.StartsWith("St ", StringComparison.OrdinalIgnoreCase) ||
                currentName.StartsWith("St-", StringComparison.OrdinalIgnoreCase);

            if (candidateLooksAbbreviated != currentLooksAbbreviated)
                return !candidateLooksAbbreviated;

            var candidateScore = GetMetadataScore(candidate);
            var currentScore = GetMetadataScore(current);

            if (candidateScore != currentScore)
                return candidateScore > currentScore;

            return candidate.Id < current.Id;
        }

        private static int GetMetadataScore(LocalAiPlaceContextDTO place)
        {
            var score = 0;

            if (!string.IsNullOrWhiteSpace(place.Type))
                score++;

            if (!string.IsNullOrWhiteSpace(place.Tag))
                score++;

            if (place.Indoor.HasValue)
                score++;

            if ((place.Capacity ?? 0) > 0)
                score++;

            if (!string.IsNullOrWhiteSpace(place.ExternalSource))
                score++;

            if (!string.IsNullOrWhiteSpace(place.ExternalId))
                score++;

            return score;
        }

        private static double? MinNullable(
            double? first,
            double? second)
        {
            if (!first.HasValue)
                return second;

            if (!second.HasValue)
                return first;

            return Math.Min(first.Value, second.Value);
        }
        private static void AppendPlaces(StringBuilder sb, LocalAiContextDTO context)
        {
            sb.AppendLine("Nearby real places:");

            if (context.Places is null || context.Places.Count == 0)
            {
                sb.AppendLine("- none");
                sb.AppendLine();
                return;
            }

            var hasConfirmedAlert = context.CriticalAlerts is not null && context.CriticalAlerts.Count > 0;

            sb.AppendLine(hasConfirmedAlert ? "Backend-approved alternatives outside confirmed alert areas:" : "Nearby candidate places:");

            foreach (var place in context.Places)
            {
                var details =
                    new List<string>
                    {
                        $"name: {place.Name}",
                        $"distance: {FmtDistance(place.DistanceKm)}"
                    };

                if (!string.IsNullOrWhiteSpace(place.Type))
                {
                    details.Add($"type: {place.Type}");
                }

                if (place.Indoor.HasValue)
                {
                    details.Add(place.Indoor.Value ? "indoor: true" : "indoor: false");
                }

                sb.AppendLine("- " + string.Join("; ", details) + ".");
            }

            sb.AppendLine();
        }

        private static void AppendEvents(StringBuilder sb, LocalAiContextDTO context)
        {
            sb.AppendLine("Nearby real events and visits:");

            if (context.Events is null || context.Events.Count == 0)
            {
                sb.AppendLine("- none");
                sb.AppendLine();
                return;
            }

            foreach (var currentEvent in context.Events)
            {
                var details = new List<string>();

                details.Add($"name: " + $"{currentEvent.Title ?? "Unknown event"}");

                if (!string.IsNullOrWhiteSpace(currentEvent.City))
                {
                    details.Add($"location: {currentEvent.City}");
                }

                if (currentEvent.EventDate.HasValue)
                {
                    details.Add($"date: " + $"{currentEvent.EventDate.Value:yyyy-MM-dd}");
                }

                if (currentEvent.StartTime.HasValue)
                {
                    var timeText = $"start time: " + $"{FmtTs(currentEvent.StartTime)}";

                    if (currentEvent.EndTime.HasValue)
                    {
                        timeText += $"; end time: " + $"{FmtTs(currentEvent.EndTime)}";
                    }

                    details.Add(timeText);
                }

                details.Add($"distance: " + $"{FmtDistance(currentEvent.DistanceKm)}");

                if (!string.IsNullOrWhiteSpace(currentEvent.Advice))
                {
                    details.Add($"setting: {currentEvent.Advice}");
                }

                sb.AppendLine(
                    "- " +
                    string.Join(
                        "; ",
                        details) +
                    ".");
            }

            sb.AppendLine();
        }

        private static void AppendCrowdCalendar(StringBuilder sb, LocalAiContextDTO context)
        {
            sb.AppendLine("Planned crowd-sensitive events:");

            if (context.CrowdCalendar is null || context.CrowdCalendar.Count == 0)
            {
                sb.AppendLine("- none");
                sb.AppendLine();
                return;
            }

            foreach (var e in context.CrowdCalendar)
            {
                sb.AppendLine(
                    $"- {e.EventName ?? "Unknown event"}, " +
                    $"{e.RegionCode ?? "Unknown region"}, " +
                    $"{(e.DateUtc?.ToString("yyyy-MM-dd") ?? "—")}, " +
                    $"{FmtTs(e.StartLocalTime)}-{FmtTs(e.EndLocalTime)}, " +
                    $"level {(e.ExpectedLevel?.ToString() ?? "—")}, " +
                    $"confidence {(e.Confidence?.ToString() ?? "—")}%, " +
                    $"{FmtDistance(e.DistanceKm)}");
            }

            sb.AppendLine();
        }

        private static void AppendCrowdInfo(StringBuilder sb, LocalAiContextDTO context)
        {
            sb.AppendLine("Recent observed crowd:");

            if (context.CrowdInfo is null || context.CrowdInfo.Count == 0)
            {
                sb.AppendLine("- none");
                sb.AppendLine();
                return;
            }

            foreach (var c in context.CrowdInfo)
            {
                sb.AppendLine(
                    $"- {c.LocationName ?? "Unknown place"}, " +
                    $"level {(c.CrowdLevel?.ToString() ?? "—")}, " +
                    $"{(c.Timestamp?.ToString("yyyy-MM-dd HH:mm") ?? "—")}, " +
                    $"{FmtDistance(c.DistanceKm)}");
            }

            sb.AppendLine();
        }

        private static void AppendTraffic(StringBuilder sb, LocalAiContextDTO context)
        {
            sb.AppendLine("Traffic with practical impact:");

            if (context.Traffic is null || context.Traffic.Count == 0)
            {
                sb.AppendLine("- none");
                sb.AppendLine();
                return;
            }

            foreach (var t in context.Traffic)
            {
                var label = t.Title ?? t.IncidentType ?? t.Road ?? "Traffic incident";

                sb.AppendLine(
                    $"- {label}, " +
                    $"severity {(t.Severity?.ToString() ?? "—")}, " +
                    $"{(t.DateCondition?.ToString("yyyy-MM-dd HH:mm") ?? "—")}, " +
                    $"{FmtDistance(t.DistanceKm)}");
            }

            sb.AppendLine();
        }

        private static void AppendWeather(StringBuilder sb, LocalAiContextDTO context)
        {
            sb.AppendLine("Weather with practical impact:");

            if (context.Weather is null || context.Weather.Count == 0)
            {
                sb.AppendLine("- none");
                sb.AppendLine();
                return;
            }

            foreach (var w in context.Weather)
            {
                sb.AppendLine(
                    $"- {(w.DateWeather?.ToString("yyyy-MM-dd HH:mm") ?? "—")}, " +
                    $"{(w.TemperatureC?.ToString() ?? "—")}°C, " +
                    $"humidity {(w.Humidity?.ToString() ?? "—")}%, " +
                    $"wind {(w.WindSpeedKmh?.ToString("0.#") ?? "—")} km/h, " +
                    $"rain {(w.RainfallMm?.ToString("0.#") ?? "—")} mm, " +
                    $"severe {(w.IsSevere?.ToString() ?? "—")}, " +
                    $"description: {w.Description ?? w.WeatherMain ?? w.Summary ?? "—"}");
            }

            sb.AppendLine();
        }

        private static void AppendCriticalAlerts(StringBuilder sb, LocalAiContextDTO context)
        {
            sb.AppendLine("Confirmed critical alerts:");

            if (context.CriticalAlerts is null || context.CriticalAlerts.Count == 0)
            {
                sb.AppendLine("- none");
                sb.AppendLine();
                return;
            }

            foreach (var a in context.CriticalAlerts)
            {
                sb.AppendLine(
                    $"- {a.AlertKind}, " +
                    $"status {a.Status}, " +
                    $"severity {a.Severity}, " +
                    $"place {a.PlaceName ?? "—"}, " +
                    $"description {a.Description ?? "—"}, " +
                    $"distance {FmtDistance(a.DistanceKm)}, " +
                    $"expires {(a.ExpiresAtUtc?.ToString("yyyy-MM-dd HH:mm") ?? "—")}");
            }

            sb.AppendLine();
        }

        private static void AppendUserSafetyConstraints(StringBuilder sb, LocalAiContextDTO context)
        {
            sb.AppendLine("User safety constraints:");

            if (context.HasChildren)
            {
                sb.AppendLine("- The user is with children.");
                sb.AppendLine("- Prefer calm, supervised, child-friendly places.");
                sb.AppendLine("- Avoid isolated, overcrowded, road-exposed, disaster-adjacent, or hazardous places.");
            }

            if (context.BadWeatherDetected)
            {
                sb.AppendLine("- Bad weather is detected.");
                sb.AppendLine("- Prefer indoor alternatives.");
                sb.AppendLine("- Avoid outdoor-only activities unless no safer indoor option exists.");
            }

            if (!context.HasChildren && !context.BadWeatherDetected)
            {
                sb.AppendLine("- none");
            }

            sb.AppendLine();
        }

        private static string FmtTs(TimeSpan? ts)
            => ts.HasValue ? ts.Value.ToString(@"hh\:mm") : "—";

        private static string FmtDistance(double? distanceKm)
            => distanceKm.HasValue ? string.Create(CultureInfo.InvariantCulture, $"{distanceKm.Value:0.0} km") : "distance non disponible";

        private static LocalAiContextIntent ResolveIntent(string? prompt)
        {
            var p = (prompt ?? string.Empty).ToLowerInvariant();

            var asksTraffic =
                p.Contains("trafic") || p.Contains("traffic") || p.Contains("bouchon") ||
                p.Contains("route") || p.Contains("accident");

            var asksWeather =
                p.Contains("météo") || p.Contains("meteo") || p.Contains("pluie") ||
                p.Contains("vent") || p.Contains("orage") || p.Contains("température") ||
                p.Contains("temperature");

            var asksCrowd =
                p.Contains("foule") || p.Contains("monde") || p.Contains("affluence") ||
                p.Contains("crowd");

            var asksEvent =
                p.Contains("événement") || p.Contains("evenement") ||
                p.Contains("activité") || p.Contains("activite") ||
                p.Contains("concert") || p.Contains("sortie") ||
                p.Contains("intéressant") || p.Contains("interessant") ||
                p.Contains("quoi faire") || p.Contains("à voir") || p.Contains("a voir");

            var asksPlaces =
                asksEvent ||
                p.Contains("lieu") ||
                p.Contains("lieux") ||
                p.Contains("endroit") ||
                p.Contains("endroits") ||
                p.Contains("visite") ||
                p.Contains("visiter") ||
                p.Contains("voir") ||
                p.Contains("découvrir") ||
                p.Contains("decouvrir") ||
                p.Contains("près de") ||
                p.Contains("proche") ||
                p.Contains("alentours") ||
                p.Contains("autour de") ||
                p.Contains("dans les environs");

            if (!asksTraffic && !asksWeather && !asksCrowd && !asksEvent && !asksPlaces)
            {
                return new LocalAiContextIntent
                {
                    NeedPlaces = true,
                    NeedEvents = true,
                    NeedCrowdCalendar = false,
                    NeedCrowdInfo = false,
                    NeedTraffic = false,
                    NeedWeather = false
                };
            }

            return new LocalAiContextIntent
            {
                NeedPlaces = asksPlaces,
                NeedEvents = asksEvent,
                NeedCrowdCalendar = asksEvent || asksCrowd,
                NeedCrowdInfo = asksCrowd || asksEvent,
                NeedTraffic = asksTraffic,
                NeedWeather = asksWeather || asksEvent
            };
        }

        private static LocalAiDateRange ResolveTargetDateRange(string? prompt)
        {
            var today = GetBelgiumToday();

            var normalized = prompt?.Trim().ToLowerInvariant() ?? string.Empty;

            var asksCurrentWeek = normalized.Contains("cette semaine") || normalized.Contains("dans la semaine");

            if (asksCurrentWeek)
            {
                var daysUntilNextMonday =
                    ((int)DayOfWeek.Monday -
                     (int)today.DayOfWeek +
                     7) % 7;

                if (daysUntilNextMonday == 0)
                    daysUntilNextMonday = 7;

                var nextMonday =
                    today.AddDays(daysUntilNextMonday);

                return new LocalAiDateRange(
                    From: today,
                    ToExclusive: nextMonday);
            }

            var asksWeekend = normalized.Contains("ce weekend") || normalized.Contains("ce week-end");

            if (asksWeekend)
            {
                DateTime saturday;

                if (today.DayOfWeek == DayOfWeek.Saturday)
                {
                    // Nous sommes déjà samedi.
                    saturday = today;
                }
                else if (today.DayOfWeek == DayOfWeek.Sunday)
                {
                    // Le week-end actuel a commencé hier.
                    saturday = today.AddDays(-1);
                }
                else
                {
                    // Prochain samedi.
                    var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;

                    saturday = today.AddDays(daysUntilSaturday);
                }

                return new LocalAiDateRange(From: saturday, ToExclusive: saturday.AddDays(2));
            }

            if (normalized.Contains("ce samedi"))
            {
                var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;

                var saturday = today.AddDays(daysUntilSaturday);

                return new LocalAiDateRange(From: saturday, ToExclusive: saturday.AddDays(1));
            }

            if (normalized.Contains("ce dimanche"))
            {
                var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)today.DayOfWeek + 7) % 7;

                var sunday = today.AddDays(daysUntilSunday);

                return new LocalAiDateRange(From: sunday, ToExclusive: sunday.AddDays(1));
            }

            if (normalized.Contains("demain"))
            {
                var tomorrow = today.AddDays(1);

                return new LocalAiDateRange(From: tomorrow, ToExclusive: tomorrow.AddDays(1));
            }

            // "today" or the absence of a time indicator.
            return new LocalAiDateRange(From: today, ToExclusive: today.AddDays(1));
        }

        private static DateTime GetBelgiumToday()
        {
            try
            {
                var belgiumTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels");

                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, belgiumTimeZone).Date;
            }
            catch (TimeZoneNotFoundException)
            {
                return DateTime.Now.Date;
            }
            catch (InvalidTimeZoneException)
            {
                return DateTime.Now.Date;
            }
        }
    }
}





























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.