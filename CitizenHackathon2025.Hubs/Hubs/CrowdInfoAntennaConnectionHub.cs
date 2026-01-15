using System;
using System.Threading.Tasks;
using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.Hubs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Hubs.Hubs
{
    [Authorize]
    public sealed class CrowdInfoAntennaConnectionHub : Hub
    {
        private readonly ILogger<CrowdInfoAntennaConnectionHub> _logger;
        private readonly ICrowdInfoAntennaService _antennaSvc;

        public CrowdInfoAntennaConnectionHub(
            ILogger<CrowdInfoAntennaConnectionHub> logger,
            ICrowdInfoAntennaService antennaSvc)
        {
            _logger = logger;
            _antennaSvc = antennaSvc;
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("CrowdInfoAntennaConnectionHub connected: {ConnId}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("CrowdInfoAntennaConnectionHub disconnected: {ConnId}", Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        // ---- Subscribe by antenna
        public Task JoinAntenna(int antennaId)
        {
            var group = CrowdInfoAntennaConnectionHubMethods.AntennaGroup(antennaId);
            _logger.LogDebug("JoinAntenna {ConnId} -> {Group}", Context.ConnectionId, group);
            return Groups.AddToGroupAsync(Context.ConnectionId, group);
        }

        public Task LeaveAntenna(int antennaId)
        {
            var group = CrowdInfoAntennaConnectionHubMethods.AntennaGroup(antennaId);
            _logger.LogDebug("LeaveAntenna {ConnId} -> {Group}", Context.ConnectionId, group);
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        }

        // ---- Subscribe by event (client wants “event -> nearest antenna -> counts” pushed)
        public Task JoinEvent(int eventId)
        {
            var group = CrowdInfoAntennaConnectionHubMethods.EventGroup(eventId);
            _logger.LogDebug("JoinEvent {ConnId} -> {Group}", Context.ConnectionId, group);
            return Groups.AddToGroupAsync(Context.ConnectionId, group);
        }

        public Task LeaveEvent(int eventId)
        {
            var group = CrowdInfoAntennaConnectionHubMethods.EventGroup(eventId);
            _logger.LogDebug("LeaveEvent {ConnId} -> {Group}", Context.ConnectionId, group);
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        }

        // Optional: on-demand pull (client can call to force refresh)
        public async Task RequestEventCrowd(int eventId, int windowMinutes = 10, double maxRadiusMeters = 5000)
        {
            var dto = await _antennaSvc.GetEventCrowdAsync(eventId, windowMinutes, maxRadiusMeters, Context.ConnectionAborted);
            await Clients.Caller.SendAsync(CrowdInfoAntennaConnectionHubMethods.ToClient.EventCrowdUpdated, dto);
        }
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.