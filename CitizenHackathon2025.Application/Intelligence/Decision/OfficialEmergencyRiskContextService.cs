using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Contracts.Enums.CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.EmergencyIntelligence.Interfaces;
using CitizenHackathon2025.EmergencyIntelligence.Models;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;

namespace CitizenHackathon2025.Application.Intelligence.Decision
{
    public sealed class OfficialEmergencyRiskContextService
    {
        private readonly IEmergencyAlertRepository _repository;
        public OfficialEmergencyRiskContextService(IEmergencyAlertRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }
        public async Task EnrichAsync(DecisionContext context, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(context);

            ct.ThrowIfCancellationRequested();

            var alerts = await _repository.GetActiveAsync(ct);

            var affecting = alerts
                .Where(x => x.IsOfficial)
                .Where(x => AffectsPoint(x, context.Latitude, context.Longitude))
                .OrderByDescending(x => ResolveSeverity(x.Severity))
                .ThenByDescending(x => IsImmediate(x.Urgency))
                .ToList();


            if (affecting.Count == 0)
            {
                return;
            }

            var primary = affecting[0];

            context.HasOfficialEmergencyRisk = true;

            context.EmergencyAlertIds = affecting
                .Select(x => x.Id)
                .Distinct()
                .ToArray();

            context.EmergencySourceCode = string.Join(",", affecting
                .Select(x => x.SourceCode)
                .Distinct(StringComparer.OrdinalIgnoreCase));

            context.OfficialEmergencySeverity = ResolveSeverity(primary.Severity);

            context.IsOfficialEmergencyImmediate = IsImmediate(primary.Urgency);

            context.OfficialInstruction = !string.IsNullOrWhiteSpace(primary.Instructions) ? primary.Instructions : primary.Description;
        }

        private static bool AffectsPoint(EmergencyAlert alert, double latitude, double longitude)
        {
            if (alert.Area is null)
            {
                return false;
            }

            /*
             * CAP circle:
             * Area = centre point,
             * RadiusMeters = radius.
             */
            if (alert.RadiusMeters is > 0)
            {
                var center = alert.Area.Coordinate;

                if (center is null)
                {
                    return false;
                }

                var distance = HaversineMeters(latitude, longitude, center.Y, center.X);

                return distance <= alert.RadiusMeters.Value;
            }

            /*
             * Polygon / MultiPolygon in SRID 4326.
             */
            var factory = new GeometryFactory(new PrecisionModel(), 4326);
            var point = factory.CreatePoint(new Coordinate(longitude, latitude));

            return alert.Area.Intersects(point);
        }

        private static byte ResolveSeverity(EmergencySeverity severity)
        {
            return severity.ToString().Trim().ToLowerInvariant()
                switch
                {
                    "extreme" => 4,
                    "critical" => 4,
                    "severe" => 3,
                    "moderate" => 2,
                    "minor" => 1,
                    "low" => 1,
                    _ => 0
                };
        }

        private static bool IsImmediate(EmergencyUrgency urgency)
        {
            return string.Equals(urgency.ToString(), "Immediate", StringComparison.OrdinalIgnoreCase);
        }
        private static double HaversineMeters(double latitude1, double longitude1, double latitude2, double longitude2)
        {
            const double EarthRadiusMeters = 6_371_000.0;

            static double ToRadians(double degrees)
            {
                return degrees
                    * Math.PI
                    / 180.0;
            }

            var lat1 = ToRadians(latitude1);
            var lat2 = ToRadians(latitude2);
            var deltaLatitude = ToRadians(latitude2 - latitude1);
            var deltaLongitude = ToRadians(longitude2 - longitude1);
            var a = Math.Sin(deltaLatitude / 2.0)
                *
                Math.Sin(deltaLatitude / 2.0)
                +
                Math.Cos(lat1)
                *
                Math.Cos(lat2)
                *
                Math.Sin(deltaLongitude / 2.0)
                *
                Math.Sin(deltaLongitude / 2.0);
            var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

            return EarthRadiusMeters * c;
        }
    }
}























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.