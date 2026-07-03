using CitizenHackathon2025.Contracts.DTOs;

namespace CitizenHackathon2025.Application.Intelligence.AlertFusion.AlertFusion
{
    public sealed class AlertFusionEngine : IAlertFusionEngine
    {
        private const double ClusterRadiusMeters = 1500;

        public Task<List<CrowdAlertCluster>> BuildClustersAsync(
            IEnumerable<CrowdSafetyAlertDTO> alerts,
            CancellationToken ct = default)
        {
            var builders = new List<AlertClusterBuilder>();

            foreach (var alert in alerts.OrderByDescending(a => a.Severity))
            {
                ct.ThrowIfCancellationRequested();

                var cluster = builders.FirstOrDefault(
                    b => b.IsNear(alert, ClusterRadiusMeters));

                if (cluster == null)
                {
                    cluster = new AlertClusterBuilder();
                    cluster.Add(alert);
                    builders.Add(cluster);
                }
                else
                {
                    cluster.Add(alert);
                }
            }

            var result = builders
                .Where(b => !b.IsEmpty)
                .Select(b => b.Build())
                .OrderByDescending(c => c.Severity)
                .ThenByDescending(c => c.TotalActiveConnections)
                .ToList();

            return Task.FromResult(result);
        }
    }
}




























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.