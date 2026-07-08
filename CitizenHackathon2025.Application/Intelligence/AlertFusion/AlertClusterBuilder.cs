using CitizenHackathon2025.Contracts.DTOs;

namespace CitizenHackathon2025.Application.Intelligence.AlertFusion.AlertFusion
{
    public sealed class AlertClusterBuilder
    {
        private readonly List<CrowdSafetyAlertDTO> _alerts = new();

        public void Add(CrowdSafetyAlertDTO alert)
        {
            if (alert != null)
                _alerts.Add(alert);
        }

        public bool IsEmpty => _alerts.Count == 0;

        public bool IsNear(
            CrowdSafetyAlertDTO alert,
            double radiusMeters)
        {
            if (alert == null || _alerts.Count == 0)
                return false;

            var centerLat = _alerts.Average(a => (double)a.Latitude);
            var centerLon = _alerts.Average(a => (double)a.Longitude);

            var distance = HaversineMeters(
                centerLat,
                centerLon,
                (double)alert.Latitude,
                (double)alert.Longitude);

            return distance <= radiusMeters;
        }

        public CrowdAlertCluster Build()
        {
            if (_alerts.Count == 0)
                throw new InvalidOperationException();

            var totalUniqueDevices = _alerts.Sum(a => a.UniqueDevices);

            return new CrowdAlertCluster
            {
                ZoneName = BuildZoneName(),

                Latitude = _alerts.Average(a => (double)a.Latitude),
                Longitude = _alerts.Average(a => (double)a.Longitude),

                Severity = _alerts.Max(a => a.Severity),

                RiskScore = 0,

                AlertCount = _alerts.Count,

                TotalActiveConnections = _alerts.Sum(a => a.ActiveConnections),

                TotalUniqueDevices = totalUniqueDevices,

                EstimatedPopulation = totalUniqueDevices,

                AntennaIds = _alerts
                    .Select(a => a.AntennaId)
                    .Distinct()
                    .ToList(),

                AlertIds = _alerts
                    .Select(a => a.Id)
                    .ToList(),

                FirstDetectedAtUtc = _alerts.Min(a => a.DetectedAtUtc),

                LastDetectedAtUtc = _alerts.Max(a => a.DetectedAtUtc),

                Status = "PendingValidation"
            };
        }

        private static double HaversineMeters(
            double lat1,
            double lon1,
            double lat2,
            double lon2)
        {
            const double R = 6371000;

            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) *
                Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) *
                Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(
                Math.Sqrt(a),
                Math.Sqrt(1 - a));

            return R * c;
        }

        private string BuildZoneName()
        {
            var mostSevere = _alerts
                .OrderByDescending(a => a.Severity)
                .ThenByDescending(a => a.ActiveConnections)
                .First();

            if (!string.IsNullOrWhiteSpace(mostSevere.Title))
                return mostSevere.Title;

            return $"Zone antennes {string.Join(", ", _alerts.Select(a => a.AntennaId).Distinct().Take(3))}";
        }

        private static double DegreesToRadians(double value)
        {
            return value * Math.PI / 180d;
        }
    }
}
























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.