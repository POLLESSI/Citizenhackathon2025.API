using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.DTOs.DTOs.Antennas;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Hubs.Services;
using Microsoft.AspNetCore.SignalR;
using System.Security.Cryptography;
using System.Text;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class AntennaSimulationService : IAntennaSimulationService
    {
        private readonly ICrowdInfoAntennaConnectionRepository _connRepo;
        private readonly ICrowdInfoAntennaService _antennaSvc;
        private readonly IHubContext<CrowdInfoAntennaConnectionHub> _hub;
        private readonly ICrowdSafetyDetectionService _safety;

        public AntennaSimulationService(ICrowdInfoAntennaConnectionRepository connRepo, ICrowdInfoAntennaService antennaSvc, IHubContext<CrowdInfoAntennaConnectionHub> hub, ICrowdSafetyDetectionService safety)
        {
            _connRepo = connRepo;
            _antennaSvc = antennaSvc;
            _hub = hub;
            _safety = safety;
        }

        public async Task SimulateAsync(
            SimulateAntennaConnectionsRequest request,
            CancellationToken ct = default)
        {
            if (request.AntennaId <= 0)
                throw new ArgumentOutOfRangeException(nameof(request.AntennaId));

            var deviceCount = Math.Clamp(request.DeviceCount, 1, 10_000);
            var durationSeconds = Math.Clamp(request.DurationSeconds, 5, 3600);
            var jitterPercent = Math.Clamp(request.JitterPercent, 0, 80);

            var now = DateTime.UtcNow;
            var salt = DateOnly.FromDateTime(now).ToString("yyyyMMdd");

            var effectiveCount = ApplyJitter(deviceCount, jitterPercent);

            if (request.BurstMode)
                effectiveCount = (int)(effectiveCount * 1.35);

            for (var i = 0; i < effectiveCount; i++)
            {
                ct.ThrowIfCancellationRequested();

                var deviceHash = CreateEphemeralDeviceHash(
                    request.AntennaId,
                    request.EventId,
                    i,
                    salt);

                await _connRepo.UpsertPingAsync(
                    antennaId: request.AntennaId,
                    eventId: request.EventId,
                    deviceHash: deviceHash,
                    ipHash: null,
                    macHash: null,
                    source: 9, // 9 = simulator
                    signalStrength: null,
                    band: "SIM",
                    additionalJson: """{"simulated":true}""",
                    ct: ct);
            }

            var counts = await _antennaSvc.GetCountsAsync(request.AntennaId, windowMinutes: Math.Max(1, durationSeconds / 60), ct);

            await _safety.EvaluateAntennaAsync(request.AntennaId, counts.ActiveConnections, counts.UniqueDevices, ct);

            await _hub.Clients
                .Group(CrowdInfoAntennaConnectionHubMethods.AntennaGroup(request.AntennaId))
                .SendAsync(
                    CrowdInfoAntennaConnectionHubMethods.ToClient.AntennaCountsUpdated,
                    new AntennaCountsUpdateDTO
                    {
                        AntennaId = request.AntennaId,
                        Counts = new AntennaCountsDTO
                        {
                            ActiveConnections = counts.ActiveConnections,
                            UniqueDevices = counts.UniqueDevices,
                            WindowStartUtc = counts.WindowStartUtc,
                            WindowEndUtc = counts.WindowEndUtc,
                            WindowMinutes = counts.WindowMinutes
                        }
                    },
                    ct);
        }

        private static int ApplyJitter(int value, int jitterPercent)
        {
            if (jitterPercent <= 0) return value;

            var min = value * (100 - jitterPercent) / 100;
            var max = value * (100 + jitterPercent) / 100;

            return Random.Shared.Next(min, max + 1);
        }

        private static byte[] CreateEphemeralDeviceHash(
            int antennaId,
            int? eventId,
            int deviceIndex,
            string dailySalt)
        {
            var secret = Encoding.UTF8.GetBytes("OUTZEN_SIMULATOR_DEV_SECRET_CHANGE_ME");

            var raw = $"SIM|ANT:{antennaId}|EV:{eventId?.ToString() ?? "none"}|IDX:{deviceIndex}|DAY:{dailySalt}";

            using var hmac = new HMACSHA256(secret);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
        }
    }
}






























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.