using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Contracts.Enums.CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.EmergencyIntelligence.Interfaces;
using CitizenHackathon2025.EmergencyIntelligence.Models;
using CitizenHackathon2025.EmergencyIntelligence.Records;
using GeoAPI.Geometries;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CitizenHackathon2025.EmergencyIntelligence.Sources.BeAlert
{
    public sealed class BeAlertCapNormalizer : IEmergencyAlertNormalizer
    {
        private readonly BeAlertCapOptions _options;
        private readonly ILogger<BeAlertCapNormalizer> _logger;

        private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

        public string SourceCode => BeAlertCapSource.Code;

        public BeAlertCapNormalizer(IOptions<BeAlertCapOptions> options, ILogger<BeAlertCapNormalizer> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public EmergencyAlert Normalize(RawEmergencyAlert raw)
        {
            ArgumentNullException.ThrowIfNull(raw);

            if (!string.Equals(raw.SourceCode, SourceCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"The BE-Alert normalizer cannot process " + $"source '{raw.SourceCode}'.");
            }

            if (string.IsNullOrWhiteSpace(raw.RawPayload))
            {
                throw new InvalidDataException("BE-Alert CAP payload is empty.");
            }
            var cap = ParseSingleAlert(raw);
            var area = ResolveArea(cap, out var radiusMeters);
            var now = DateTimeOffset.UtcNow;
            var referencedExternalIds = ParseReferenceIds(cap.References);

            var normalized = new EmergencyAlert
            {
                Id = Guid.NewGuid(),
                SourceCode = raw.SourceCode,
                ExternalId = raw.ExternalId,
                ExternalReferenceId = referencedExternalIds.FirstOrDefault(),
                ReferencedExternalIds = referencedExternalIds,
                CorrelationKey = $"{raw.SourceCode}:{raw.ExternalId}",
                HazardType = ResolveHazardType(cap),
                Severity = ResolveSeverity(cap.Severity),
                Urgency = ResolveUrgency(cap.Urgency),
                Certainty = ResolveCertainty(cap.Certainty),
                Status = ResolveStatus(cap, now),
                InformationKind = ResolveInformationKind(),
                Headline = FirstNonEmpty(cap.Headline, cap.Event, "Alerte officielle BE-Alert"),
                Description = FirstNonEmpty(cap.Description, cap.Headline, cap.Event, "Alerte officielle BE-Alert."),
                Instructions = cap.Instruction,
                Language = FirstNonEmpty(cap.Language, _options.PreferredLanguage, "fr-BE"),
                SentAtUtc = cap.Sent,
                EffectiveFromUtc = cap.Effective ?? cap.Sent,
                ExpiresAtUtc = cap.Expires,
                LastUpdatedAtUtc = cap.Sent,
                Area = area,
                RadiusMeters = radiusMeters,
                /*
                    * ProvinceCode / MunicipalityCode
                    * can be enriched later
                    * from CAP geocodes.
                    */
                ProvinceCode = null,
                MunicipalityCode = null,

                /*
                    * For now we keep
                    * the official URL used as
                    * the source.
                    */
                OfficialInformationUri = raw.SourceUri,
                /*
                    * The source is official.
                    */
                IsOfficial = true,
                /*
                    * Important :
                    * downloading from
                    * publicalerts.be does not yet constitute
                    * a cryptographic verification of the message.
                    */
                IsMachineVerified = false,
                PayloadHash = ComputeSha256(raw.RawPayload),
                RawPayloadStorageKey = null,
                CreatedAtUtc = raw.ReceivedAtUtc,
                UpdatedAtUtc = now
            };

            _logger.LogInformation("[BE-ALERT NORMALIZER] " + "Identifier={Identifier}, " + "Severity={Severity}, " + "Urgency={Urgency}, " + "Status={Status}, " + "HasArea={HasArea}, " + "RadiusMeters={RadiusMeters}.",
                normalized.ExternalId,
                normalized.Severity,
                normalized.Urgency,
                normalized.Status,
                normalized.Area is not null,
                normalized.RadiusMeters);

            return normalized;
        }

        // =================================================
        // CAP PARSING
        // =================================================

        private static BeAlertCapMessage ParseSingleAlert(RawEmergencyAlert raw)
        {
            var bytes = Encoding.UTF8.GetBytes(raw.RawPayload);
            using var stream = new MemoryStream(bytes);
            var messages = BeAlertCapParser.Parse(stream);


            var cap = messages.FirstOrDefault(x => string.Equals(x.Identifier, raw.ExternalId, StringComparison.Ordinal))
                ?? messages.FirstOrDefault();


            if (cap is null)
            {
                throw new InvalidDataException($"No CAP alert could be parsed for " + $"'{raw.ExternalId}'.");
            }


            return cap;
        }


        // =================================================
        // GEOMETRY
        // =================================================

        private Geometry? ResolveArea(BeAlertCapMessage cap, out double? radiusMeters)
        {
            radiusMeters = null;
            /*
             * Prefer polygons.
             */
            foreach (var polygonRaw in cap.Areas.SelectMany(x => x.Polygons))
            {
                var polygon = BeAlertCapGeometryParser.ParsePolygon(polygonRaw, GeometryFactory);

                /*
                 * Your current NTS version exposes
                 * IPolygon from GeometryFactory.
                 * Runtime implementation is still
                 * normally an NTS Geometry.
                 */
                if (polygon is Geometry geometry)
                {
                    geometry.SRID = 4326;
                    return geometry;
                }
            }

            /*
             * No polygon:
             * try a CAP circle.
             *
             * CAP circle:
             *
             * latitude,longitude radiusInKilometres
             */
            foreach (var circleRaw in cap.Areas.SelectMany(x => x.Circles))
            {
                var point = ParseCircle(circleRaw, out var radius);

                if (point is not null)
                {
                    radiusMeters = radius;

                    return point;
                }
            }
            return null;
        }


        private static Geometry? ParseCircle(string raw, out double? radiusMeters)
        {
            radiusMeters = null;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length != 2)
                return null;

            var coordinateParts = parts[0].Split(',', StringSplitOptions.TrimEntries);

            if (coordinateParts.Length != 2)
                return null;


            if (!double.TryParse(coordinateParts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)
                || !double.TryParse(coordinateParts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude)
                || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var radiusKm))
            {
                return null;
            }

            if (latitude is < -90 or > 90 || longitude is < -180 or > 180 || radiusKm <= 0)
            {
                return null;
            }

            radiusMeters = radiusKm * 1000.0;

            var point = GeometryFactory.CreatePoint( new Coordinate(longitude, latitude));

            if (point is Geometry geometry)
            {
                geometry.SRID =4326;

                return geometry;
            }


            return null;
        }


        // =================================================
        // CAP -> OUTZEN ENUMS
        // =================================================

        private static EmergencySeverity ResolveSeverity(string? raw)
        {
            return raw?.Trim().ToLowerInvariant() switch
            {
                "extreme" => EnumValue<EmergencySeverity>("Extreme", "Critical"),
                "severe" => EnumValue<EmergencySeverity>("Severe"),
                "moderate" => EnumValue<EmergencySeverity>("Moderate"),
                "minor" => EnumValue<EmergencySeverity>("Minor", "Low"),
                _ => EnumValue<EmergencySeverity>("Unknown")
            };
        }

        private static EmergencyUrgency ResolveUrgency(string? raw)
        {
            return raw?.Trim().ToLowerInvariant() switch
            {
                "immediate" => EnumValue<EmergencyUrgency>("Immediate"),
                "expected" => EnumValue<EmergencyUrgency>("Expected"),
                "future" => EnumValue<EmergencyUrgency>("Future"),
                "past" => EnumValue<EmergencyUrgency>("Past"),
                _ => EnumValue<EmergencyUrgency>("Unknown")
            };
        }

        private static EmergencyCertainty ResolveCertainty(string? raw)
        {
            return raw?.Trim().ToLowerInvariant() switch
            {
                "observed" => EnumValue<EmergencyCertainty>("Observed"),
                "likely" => EnumValue<EmergencyCertainty>("Likely"),
                "possible" => EnumValue<EmergencyCertainty>("Possible"),
                "unlikely" => EnumValue<EmergencyCertainty>("Unlikely"),
                _ => EnumValue<EmergencyCertainty>("Unknown")
            };
        }


        private static EmergencyAlertStatus ResolveStatus(BeAlertCapMessage cap, DateTimeOffset now)
        {
            /*
             * CAP msgType controls lifecycle.
             */
            if (string.Equals(cap.MessageType, "Cancel", StringComparison.OrdinalIgnoreCase))
            {
                return RequiredEnumValue<EmergencyAlertStatus>("Cancelled", "Canceled");
            }

            if (cap.Expires.HasValue && cap.Expires.Value <= now)
            {
                return RequiredEnumValue<EmergencyAlertStatus>("Expired");
            }
            /*
             * Alert / Update remain active.
             */
            return RequiredEnumValue<EmergencyAlertStatus>("Active");
        }


        private static SafetyInformationKind ResolveInformationKind()
        {
            /*
             * This member already exists in your
             * current contracts.
             */
            return RequiredEnumValue<SafetyInformationKind>("ActiveEmergency");
        }


        private static EmergencyHazardType ResolveHazardType(BeAlertCapMessage cap)
        {
            var text = string.Join(' ', cap.Event, cap.Headline, cap.Description).ToLowerInvariant();

            if (ContainsAny(text, "flood", "inond", "overstrom"))
            {
                return EnumValue<EmergencyHazardType>("Flood");
            }

            if (ContainsAny(text, "fire", "incend", "brand"))
            {
                return EnumValue<EmergencyHazardType>("Fire");
            }

            if (ContainsAny(text, "storm", "tempête", "tempete", "orage", "onweer"))
            {
                return EnumValue<EmergencyHazardType>("Storm");
            }

            if (ContainsAny(text, "explosion"))
            {
                return EnumValue<EmergencyHazardType>("Explosion");
            }

            if (ContainsAny(text, "chemical", "chimique", "toxique", "toxic", "hazardous material"))
            {
                return EnumValue<EmergencyHazardType>("HazardousMaterial");
            }

            return EnumValue<EmergencyHazardType>("Unknown");
        }


        // =================================================
        // HELPERS
        // =================================================

        private static TEnum EnumValue<TEnum>(params string[] candidates) where TEnum : struct, Enum
        {
            foreach (var candidate in candidates)
            {
                if (Enum.TryParse<TEnum>(candidate, ignoreCase: true, out var result))
                {
                    return result;
                }
            }
            return default;
        }
        private static TEnum RequiredEnumValue<TEnum>(params string[] candidates) where TEnum : struct, Enum
        {
            foreach (var candidate in candidates)
            {
                if (Enum.TryParse<TEnum>(candidate, ignoreCase: true, out var result))
                {
                    return result;
                }
            }


            throw new InvalidOperationException($"None of the expected values " + $"[{string.Join(", ", candidates)}] " + $"exists in enum {typeof(TEnum).Name}.");
        }


        private static bool ContainsAny(string value, params string[] candidates)
        {
            return candidates.Any(x => value.Contains(x, StringComparison.OrdinalIgnoreCase));
        }
        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }
            return string.Empty;
        }


        private static string ComputeSha256(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var hash = SHA256.HashData(bytes);

            return Convert.ToHexString(hash);
        }

        private static IReadOnlyList<string> ParseReferenceIds(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();

            return raw
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(reference => reference.Split(',', StringSplitOptions.TrimEntries))
                .Where(parts => parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
                .Select(parts => parts[1])
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
    }
}















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.