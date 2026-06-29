using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class TrafficConditionNormalizer : ITrafficConditionNormalizer
    {
        public string NormalizeCongestion(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "1";

            var v = value.Trim().ToLowerInvariant();

            return v switch
            {
                "1" or "low" or "freeflow" or "free-flow" or "free flow" or "fluid" or "fluide"
                    => "1",

                "2" or "medium" or "moderate" or "modéré" or "modere" or "slow" or "dense"
                    => "2",

                "3" or "high" or "heavy" or "lourd" or "busy" or "congested" or "congestion"
                    => "3",

                "4" or "jammed" or "critical" or "blocked" or "closed" or "closure" or "fermé" or "ferme" or "bloqué" or "bloque"
                    => "4",

                _ => value.Trim()
            };
        }

        public string NormalizeIncidentType(string? value, string? provider = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown";

            var v = RemoveDiacritics(value.Trim()).ToLowerInvariant();

            if (v.Contains("accident"))
                return "Accident";

            if (v.Contains("avarie") ||
                v.Contains("damage") ||
                v.Contains("schade") ||
                v.Contains("breakdown") ||
                v.Contains("panne"))
                return "Breakdown";

            if (v.Contains("weather") ||
                v.Contains("meteo") ||
                v.Contains("weersomstandigheden") ||
                v.Contains("rain") ||
                v.Contains("snow") ||
                v.Contains("storm"))
                return "Weather";

            if (v.Contains("roadworks") ||
                v.Contains("works") ||
                v.Contains("travaux") ||
                v.Contains("werkzaamheden"))
                return "Roadworks";

            if (v.Contains("crowd") ||
                v.Contains("affluence") ||
                v.Contains("reizigersstroom") ||
                v.Contains("passenger flow"))
                return "CrowdFlow";

            if (v.Contains("closure") ||
                v.Contains("closed") ||
                v.Contains("fermeture") ||
                v.Contains("ferme") ||
                v.Contains("blocked"))
                return "RoadClosed";

            if (v.Contains("statistics") ||
                v.Contains("statistique") ||
                v.Contains("accidentrisk"))
                return "AccidentRiskStatistics";

            return ToPascalSafe(value.Trim());
        }

        public string NormalizeProvider(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "manual";

            var v = value.Trim().ToLowerInvariant();

            return v switch
            {
                "odwb" => "odwb",
                "odwb-walstat" or "walstat" => "odwb-walstat",
                "perex" => "perex",
                "waze" or "waze-for-cities" => "waze",
                "here" => "here",
                "tomtom" or "tom-tom" => "tomtom",
                "manual" or "user" or "manual-alert" => "manual",
                "signalr" or "realtime" => "signalr",
                _ => v
            };
        }

        public byte? NormalizeSeverity(
            string? congestionLevel,
            byte? severity = null,
            string? incidentType = null)
        {
            if (severity is >= 1 and <= 4)
                return severity;

            var congestion = NormalizeCongestion(congestionLevel);

            if (byte.TryParse(congestion, out var level) && level is >= 1 and <= 4)
                return level;

            var incident = NormalizeIncidentType(incidentType);

            return incident switch
            {
                "RoadClosed" => 4,
                "Accident" => 3,
                "Weather" => 3,
                "Roadworks" => 2,
                "Breakdown" => 2,
                "CrowdFlow" => 2,
                "AccidentRiskStatistics" => 2,
                _ => 1
            };
        }

        public string? NormalizeRoad(string? value)
        {
            var normalized = NormalizeText(value);

            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            return normalized;
        }

        public string? NormalizeLocation(string? value)
        {
            var normalized = NormalizeText(value);

            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            return normalized;
        }

        public TrafficCondition Normalize(TrafficCondition trafficCondition)
        {
            ArgumentNullException.ThrowIfNull(trafficCondition);

            trafficCondition.Provider = NormalizeProvider(trafficCondition.Provider);
            trafficCondition.CongestionLevel = NormalizeCongestion(trafficCondition.CongestionLevel);
            trafficCondition.IncidentType = NormalizeIncidentType(
                trafficCondition.IncidentType,
                trafficCondition.Provider);

            trafficCondition.Road = NormalizeRoad(trafficCondition.Road);
            trafficCondition.Title = NormalizeText(trafficCondition.Title);
            trafficCondition.Severity = NormalizeSeverity(
                trafficCondition.CongestionLevel,
                trafficCondition.Severity,
                trafficCondition.IncidentType);

            if (trafficCondition.DateCondition == default)
                trafficCondition.DateCondition = DateTime.UtcNow;

            trafficCondition.DateCondition = trafficCondition.DateCondition.Kind == DateTimeKind.Utc
                ? trafficCondition.DateCondition
                : trafficCondition.DateCondition.ToUniversalTime();

            if (trafficCondition.LastSeenAt == default)
                trafficCondition.LastSeenAt = DateTime.UtcNow;

            trafficCondition.LastSeenAt = trafficCondition.LastSeenAt.Kind == DateTimeKind.Utc
                ? trafficCondition.LastSeenAt
                : trafficCondition.LastSeenAt.ToUniversalTime();

            trafficCondition.Active = true;

            return trafficCondition;
        }

        private static string? NormalizeText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var cleaned = Regex.Replace(value.Trim(), @"\s+", " ");

            if (cleaned is "-" or "--" or "N/A" or "n/a")
                return null;

            return cleaned;
        }

        private static string ToPascalSafe(string value)
        {
            var clean = RemoveDiacritics(value);
            clean = Regex.Replace(clean, @"[^a-zA-Z0-9]+", " ");

            var parts = clean
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
                return "Unknown";

            return string.Concat(parts.Select(p =>
                char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var chars = normalized
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray();

            return new string(chars).Normalize(NormalizationForm.FormC);
        }
    }
}










































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.