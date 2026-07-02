using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs.Antennas;
using CitizenHackathon2025.Hubs.Services;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class AntennaZoneSimulationService : IAntennaZoneSimulationService
    {
        private readonly ICrowdInfoAntennaRepository _antennaRepo;
        private readonly IAntennaSimulationService _antennaSimulator;
        private readonly ILogger<AntennaZoneSimulationService> _logger;

        public AntennaZoneSimulationService(ICrowdInfoAntennaRepository antennaRepo, IAntennaSimulationService antennaSimulator, ILogger<AntennaZoneSimulationService> logger)
        {
            _antennaRepo = antennaRepo;
            _antennaSimulator = antennaSimulator;
            _logger = logger;
        }

        public async Task SimulateAsync(
            SimulateAntennaZoneRequest request,
            CancellationToken ct = default)
        {
            if (request.DeviceCount is < 1 or > 10_000)
                throw new ArgumentOutOfRangeException(nameof(request.DeviceCount));

            if (request.CenterLatitude is < -90 or > 90)
                throw new ArgumentOutOfRangeException(nameof(request.CenterLatitude));

            if (request.CenterLongitude is < -180 or > 180)
                throw new ArgumentOutOfRangeException(nameof(request.CenterLongitude));

            var radiusDegrees = request.RadiusMeters / 111_000d;

            var antennas = await _antennaRepo.GetByBoundsAsync(
                request.CenterLatitude - radiusDegrees,
                request.CenterLatitude + radiusDegrees,
                request.CenterLongitude - radiusDegrees,
                request.CenterLongitude + radiusDegrees,
                ct);

            var selected = antennas
                .Take(4)
                .ToList();

            if (selected.Count == 0)
                throw new InvalidOperationException("No antenna found in the requested simulation zone.");

            var weights = BuildDistribution(selected.Count);

            for (var i = 0; i < selected.Count; i++)
            {
                var antenna = selected[i];
                var ratio = weights[i];

                var deviceCount = Math.Max(
                    1,
                    (int)Math.Round(request.DeviceCount * ratio));

                await _antennaSimulator.SimulateAsync(
                    new SimulateAntennaConnectionsRequest
                    {
                        AntennaId = antenna.Id,
                        EventId = request.EventId,
                        DeviceCount = deviceCount,
                        DurationSeconds = request.DurationSeconds,
                        JitterPercent = request.JitterPercent,
                        BurstMode = request.BurstMode
                    },
                    ct);

                _logger.LogInformation(
                    "Zone simulation sent to antenna {AntennaId}. Devices={DeviceCount}, Ratio={Ratio}",
                    antenna.Id,
                    deviceCount,
                    ratio);
            }
        }

        private static IReadOnlyList<double> BuildDistribution(int antennaCount)
        {
            return antennaCount switch
            {
                <= 0 => Array.Empty<double>(),
                1 => new[] { 1.00 },
                2 => new[] { 0.70, 0.30 },
                3 => new[] { 0.55, 0.30, 0.15 },
                _ => new[] { 0.55, 0.25, 0.15, 0.05 }
            };
        }
    }
}





























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.