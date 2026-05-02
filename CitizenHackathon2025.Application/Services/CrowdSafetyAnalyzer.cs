using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Models;

namespace CitizenHackathon2025.Application.Services
{
    public sealed class CrowdSafetyAnalyzer : ICrowdSafetyAnalyzer
    {
        public byte ComputeSeverity(CrowdSafetyContext ctx)
        {
            if (ctx.UniqueDevices < 20)
                return 0;

            var severity = ComputeBaseSeverity(
                ctx.ActiveConnections,
                ctx.UniqueDevices,
                ctx.MaxCapacity,
                ctx.BaselineConnections);

            if (ctx.IsNight)
                severity++;

            if (ctx.IsRural)
                severity++;

            if (!ctx.IsKnownEvent)
                severity++;

            if (ctx.IsSensitiveZone)
                severity++;

            if (ctx.IsPersistent)
                severity++;

            return (byte)Math.Clamp(severity, 0, 4);
        }

        private static int ComputeBaseSeverity(
            int activeConnections,
            int uniqueDevices,
            int? maxCapacity,
            int? baselineConnections)
        {
            if (uniqueDevices < 20)
                return 0;

            if (maxCapacity is > 0)
            {
                var ratio = activeConnections / (double)maxCapacity.Value;

                return ratio switch
                {
                    >= 1.20 => 4,
                    >= 0.90 => 3,
                    >= 0.70 => 2,
                    >= 0.50 => 1,
                    _ => 0
                };
            }

            if (baselineConnections is > 0)
            {
                var spike = activeConnections / (double)baselineConnections.Value;

                return spike switch
                {
                    >= 5.0 => 4,
                    >= 3.0 => 3,
                    >= 2.0 => 2,
                    >= 1.5 => 1,
                    _ => 0
                };
            }

            return activeConnections switch
            {
                >= 500 => 4,
                >= 250 => 3,
                >= 100 => 2,
                >= 50 => 1,
                _ => 0
            };
        }
    }
}




































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.