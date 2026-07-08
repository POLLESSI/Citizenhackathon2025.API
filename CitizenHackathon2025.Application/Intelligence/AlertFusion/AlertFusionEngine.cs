using CitizenHackathon2025.Contracts.DTOs;

namespace CitizenHackathon2025.Application.Intelligence.AlertFusion
{
    public sealed class AlertFusionEngine : IAlertFusionEngine
    {
        private const double ClusterRadiusMeters = 1500;

        public Task<List<CrowdAlertCluster>> BuildClustersAsync(
            IEnumerable<CrowdSafetyAlertDTO> alerts,
            CancellationToken ct = default)
        {
            var clusters = new List<List<CrowdSafetyAlertDTO>>();

            foreach (var alert in alerts
                         .Where(a => a.Active)
                         .OrderByDescending(a => a.Severity))
            {
                ct.ThrowIfCancellationRequested();

                var existing = clusters.FirstOrDefault(c =>
                    c.Any(x => DistanceMeters(
                        (double)x.Latitude,
                        (double)x.Longitude,
                        (double)alert.Latitude,
                        (double)alert.Longitude) <= ClusterRadiusMeters));

                if (existing is null)
                    clusters.Add(new List<CrowdSafetyAlertDTO> { alert });
                else
                    existing.Add(alert);
            }

            var result = clusters.Select(BuildCluster)
                .OrderByDescending(c => c.RiskScore)
                .ToList();

            return Task.FromResult(result);
        }

        private static CrowdAlertCluster BuildCluster(List<CrowdSafetyAlertDTO> alerts)
        {
            var totalConnections = alerts.Sum(a => a.ActiveConnections);
            var totalUnique = alerts.Sum(a => a.UniqueDevices);
            var maxSeverity = alerts.Max(a => a.Severity);

            var lat = alerts.Average(a => (double)a.Latitude);
            var lng = alerts.Average(a => (double)a.Longitude);

            return new CrowdAlertCluster
            {
                ZoneName = ResolveZoneName(alerts),
                Latitude = lat,
                Longitude = lng,
                Severity = maxSeverity,
                AlertCount = alerts.Count,
                TotalActiveConnections = totalConnections,
                TotalUniqueDevices = totalUnique,
                EstimatedPopulation = totalUnique,
                RiskScore = ComputeBaseRisk(maxSeverity, totalConnections, alerts.Count),
                FirstDetectedAtUtc = alerts.Min(a => a.DetectedAtUtc),
                LastDetectedAtUtc = alerts.Max(a => a.DetectedAtUtc),
                AlertIds = alerts.Select(a => a.Id).ToList(),
                AntennaIds = alerts.Select(a => a.AntennaId).Distinct().ToList()
            };
        }

        private static string ResolveZoneName(List<CrowdSafetyAlertDTO> alerts)
        {
            var first = alerts.OrderByDescending(a => a.Severity).First();

            if (!string.IsNullOrWhiteSpace(first.Title))
                return first.Title;

            return $"Zone antenne {first.AntennaId}";
        }

        private static int ComputeBaseRisk(byte severity, int connections, int alertCount)
        {
            var score = severity switch
            {
                >= 4 => 80,
                3 => 65,
                2 => 45,
                1 => 25,
                _ => 10
            };

            score += Math.Min(10, alertCount * 2);
            score += connections switch
            {
                >= 2000 => 10,
                >= 1000 => 8,
                >= 500 => 5,
                >= 250 => 3,
                _ => 0
            };

            return Math.Clamp(score, 0, 100);
        }

        private static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double r = 6371000;
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);

            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRad(double value) => value * Math.PI / 180d;
    }
}




























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.