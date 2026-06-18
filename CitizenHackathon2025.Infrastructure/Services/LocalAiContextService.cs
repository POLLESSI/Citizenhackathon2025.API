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

        private static readonly LocalAiContextLimits Limits = new();

        public LocalAiContextService(
            ILocalAiDataRepository localAiRepo,
            ILogger<LocalAiContextService> logger)
        {
            _localAiRepo = localAiRepo ?? throw new ArgumentNullException(nameof(localAiRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<LocalAiContextDTO> BuildContextAsync(
            string prompt,
            double? latitude,
            double? longitude,
            CancellationToken ct = default)
        {
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
            var targetDate = ResolveTargetDate(safePrompt);
            var intent = ResolveIntent(safePrompt);
            var requestedPlace = keywordPlaces.FirstOrDefault();

            if (requestedPlace is not null)
            {
                lat = requestedPlace.Latitude ?? lat;
                lng = requestedPlace.Longitude ?? lng;

                locationLabel = requestedPlace.Name;
            }

            _logger.LogInformation(
                "Building local AI context. PromptLength={PromptLength}, Lat={Lat}, Lng={Lng}, RadiusKm={RadiusKm}, TargetDate={TargetDate:yyyy-MM-dd}, Intent={@Intent}",
                safePrompt.Length,
                lat,
                lng,
                radiusKm,
                targetDate,
                intent);

            Task<IEnumerable<LocalAiPlaceContextDTO>> placesTask = intent.NeedPlaces
                ? _localAiRepo.GetNearbyPlacesAsync(lat, lng, radiusKm, ct)
                : Task.FromResult(Enumerable.Empty<LocalAiPlaceContextDTO>());

            Task<IEnumerable<LocalAiEventContextDTO>> eventsTask = intent.NeedEvents
                ? _localAiRepo.GetNearbyEventsAsync(lat, lng, targetDate, radiusKm, ct)
                : Task.FromResult(Enumerable.Empty<LocalAiEventContextDTO>());

            Task<IEnumerable<LocalAiCrowdCalendarContextDTO>> crowdCalendarTask = intent.NeedCrowdCalendar
                ? _localAiRepo.GetNearbyCrowdCalendarAsync(lat, lng, targetDate, radiusKm, ct)
                : Task.FromResult(Enumerable.Empty<LocalAiCrowdCalendarContextDTO>());

            Task<IEnumerable<LocalAiCrowdInfoContextDTO>> crowdInfoTask = intent.NeedCrowdInfo
                ? _localAiRepo.GetNearbyCrowdInfoAsync(lat, lng, targetDate, radiusKm, ct)
                : Task.FromResult(Enumerable.Empty<LocalAiCrowdInfoContextDTO>());

            Task<IEnumerable<LocalAiTrafficContextDTO>> trafficTask = intent.NeedTraffic
                ? _localAiRepo.GetNearbyTrafficAsync(lat, lng, targetDate, radiusKm, ct)
                : Task.FromResult(Enumerable.Empty<LocalAiTrafficContextDTO>());

            Task<IEnumerable<LocalAiWeatherContextDTO>> weatherTask = intent.NeedWeather
                ? _localAiRepo.GetNearbyWeatherAsync(lat, lng, targetDate, radiusKm, ct)
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

            var places = keywordPlaces
                .Concat(nearbyPlaces)
                .GroupBy(p => p.Id)
                .Select(g => g.First())
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
                TargetDate = targetDate,
                Places = places,
                Events = events,
                CrowdCalendar = crowdCalendar,
                CrowdInfo = crowdInfo,
                Traffic = traffic,
                Weather = weather,
                CriticalAlerts = criticalAlerts,
                LocationLabel = locationLabel,
                KeywordMatchedPlaces = keywordPlaces.ToList(),
                HasChildren = hasChildren,
                BadWeatherDetected = badWeatherDetected,
                MaxAlternativeRadiusKm = 25
            };
        }

        public string BuildPrompt(LocalAiContextDTO context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var sb = new StringBuilder(4096);

            sb.AppendLine("You are OutZen local assistant.");
            sb.AppendLine("Answer only with facts present in the provided context.");
            sb.AppendLine("Do not invent places, visits, events, traffic, weather, or crowd data.");
            sb.AppendLine("Do not choose the response language here.");
            sb.AppendLine("The response language is controlled by the system message.");
            sb.AppendLine("Be concise, concrete, and useful.");
            sb.AppendLine();

            sb.AppendLine("Priority rules:");
            sb.AppendLine("1. Prioritize real nearby places from the places dataset.");
            sb.AppendLine("2. Then use real nearby events if available.");
            sb.AppendLine("3. Mention planned or observed crowd only if it helps the user choose where or when to go.");
            sb.AppendLine("4. Mention traffic only if it materially affects access or comfort.");
            sb.AppendLine("5. Mention weather only if it has a significant practical impact on the outing.");
            sb.AppendLine("6. Weather is never a point of interest by itself.");
            sb.AppendLine("7. Never present a raw weather observation as something interesting to see or do.");
            sb.AppendLine("8. If no real nearby place or event is available, say it clearly.");
            sb.AppendLine("9. If data is missing, say it clearly.");
            sb.AppendLine();

            sb.AppendLine("Critical safety rules:");
            sb.AppendLine("1. If a destination is affected by a confirmed Crowd, Weather, Traffic, or Disaster alert, do not recommend it.");
            sb.AppendLine("2. Propose safer alternatives outside the affected zone.");
            sb.AppendLine("3. Safety has priority over distance.");
            sb.AppendLine("4. Alternatives may be up to 20-25 km away, including outside Wallonia if safer.");
            sb.AppendLine("5. Do not route users toward an alert zone.");
            sb.AppendLine("6. Never increase crowd concentration near a critical alert.");
            sb.AppendLine("7. Clearly explain why the original destination is not recommended.");
            sb.AppendLine();

            sb.AppendLine("Response style:");
            sb.AppendLine("- start with the most relevant real nearby place if one exists");
            sb.AppendLine("- if no place is available, use the most relevant real nearby event");
            sb.AppendLine("- if none exists, explicitly say that no precise nearby place or event was found in the available data");
            sb.AppendLine("- add crowd, traffic, or weather only as practical context");
            sb.AppendLine("- do not turn temperature, rain, wind, or weather measurements into attractions");
            sb.AppendLine("- if there are several relevant nearby items, mention the closest or most useful ones first");
            sb.AppendLine();

            sb.AppendLine($"Question: {context.UserPrompt}");
            sb.AppendLine($"Date: {context.TargetDate:yyyy-MM-dd}");
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
            sb.AppendLine("- real places first");
            sb.AppendLine("- real events second");
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
            sb.AppendLine();

            sb.AppendLine("Child-safety factuality rules:");
            sb.AppendLine("- Do not say a place is supervised unless the context explicitly says supervised=true or the type/tag proves it.");
            sb.AppendLine("- Do not say a place is calm unless the context explicitly says calm, quiet, low crowd, or low risk.");
            sb.AppendLine("- If the context only says city/village, describe it as a safer fallback area, not as a supervised child-friendly place.");
            sb.AppendLine("- Prefer wording like: 'zone de repli plus sûre selon les données disponibles'.");
            sb.AppendLine();

            sb.AppendLine("Factuality rules for alternatives:");
            sb.AppendLine("- Do not claim that a place is indoor unless the context explicitly says indoor=true or type/tag indicates an indoor venue.");
            sb.AppendLine("- Do not claim that a place is child-friendly unless the context explicitly says child-friendly, family, enfant, famille, playground, museum, indoor, supervised, or similar.");
            sb.AppendLine("- If the context only says city/village, say it is a nearby fallback area, not a guaranteed child-friendly attraction.");
            sb.AppendLine("- Never invent attractions, parks, museums, castles, indoor facilities, restaurants, or distances.");
            sb.AppendLine();

            sb.AppendLine("Answer format:");
            sb.AppendLine("1. Start with a short safety warning if the requested destination is affected by an alert.");
            sb.AppendLine("2. Recommend 1 to 3 alternatives only from Safe candidate alternatives.");
            sb.AppendLine("3. For each alternative, give distance and only facts explicitly present in the context.");
            sb.AppendLine("4. If indoor or child-friendly is unknown, say it is a safer nearby fallback area, not a guaranteed indoor/child-friendly activity.");
            sb.AppendLine("5. Do not mention attractions that are not present in the context.");
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
            sb.AppendLine();

            return sb.ToString();
        }

        private static double NormalizeLatitude(double? latitude)
            => latitude ?? DefaultLatitude;

        private static double NormalizeLongitude(double? longitude)
            => longitude ?? DefaultLongitude;

        private static double NormalizeRadiusKm(double radiusKm)
            => radiusKm > 0d ? radiusKm : 25d;

        //private static LocalAiPlaceContextDTO MapPlaceToLocalAiPlaceContext(Place p)
        //{
        //    return new LocalAiPlaceContextDTO
        //    {
        //        Id = p.Id,
        //        Name = p.Name?.Trim() ?? string.Empty,
        //        Type = p.Type,
        //        Indoor = p.Indoor,
        //        Latitude = (double?)p.Latitude,
        //        Longitude = (double?)p.Longitude,
        //        Capacity = p.Capacity,
        //        Tag = p.Tag,
        //        ExternalSource = p.ExternalSource,
        //        ExternalId = p.ExternalId,
        //        SourceUpdatedAtUtc = p.SourceUpdatedAtUtc,
        //        Active = true,
        //        DistanceKm = null
        //    };
        //}

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
        private static void AppendPlaces(StringBuilder sb, LocalAiContextDTO context)
        {
            sb.AppendLine("Nearby real places:");

            if (context.Places is null || context.Places.Count == 0)
            {
                sb.AppendLine("- none");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("Safe candidate alternatives:");
            sb.AppendLine("Only recommend places from this list.");

            foreach (var p in context.Places)
            {
                sb.AppendLine(
                    $"- name: {p.Name}; " +
                    $"distanceKm: {(p.DistanceKm?.ToString("0.0", CultureInfo.InvariantCulture) ?? "unknown")}; " +
                    $"type: {p.Type ?? "unknown"}; " +
                    $"indoor: {(p.Indoor == true ? "true" : p.Indoor == false ? "false" : "unknown")}; " +
                    $"tag: {p.Tag ?? "none"}; " +
                    $"capacity: {(p.Capacity?.ToString() ?? "unknown")}; " +
                    $"safetyStatus: backend-filtered-safe");
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

            foreach (var e in context.Events)
            {
                sb.AppendLine(
                    $"- {e.Title ?? "Unknown event"}, " +
                    $"{e.City ?? "Unknown city"}, " +
                    $"{(e.EventDate?.ToString("yyyy-MM-dd") ?? "—")}, " +
                    $"{FmtTs(e.StartTime)}-{FmtTs(e.EndTime)}, " +
                    $"crowd {(e.CrowdLevel?.ToString() ?? "—")}, " +
                    $"capacity {(e.MaxCapacity?.ToString() ?? "—")}, " +
                    $"{FmtDistance(e.DistanceKm)}, " +
                    $"advice: {e.Advice ?? "—"}");
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
                    NeedCrowdCalendar = true,
                    NeedCrowdInfo = true,
                    NeedTraffic = true,
                    NeedWeather = true
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

        private static DateTime ResolveTargetDate(string? prompt)
        {
            var now = DateTime.Now;
            var p = prompt?.ToLowerInvariant() ?? string.Empty;

            if (p.Contains("ce weekend") || p.Contains("ce week-end"))
            {
                var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)now.DayOfWeek + 7) % 7;
                return now.Date.AddDays(daysUntilSaturday == 0 ? 0 : daysUntilSaturday);
            }

            if (p.Contains("demain"))
                return now.Date.AddDays(1);

            if (p.Contains("aujourd"))
                return now.Date;

            return now.Date;
        }
    }
}





























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.