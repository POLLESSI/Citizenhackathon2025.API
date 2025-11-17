using System.Threading.Tasks;
using CitizenHackathon2025.Contracts.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Hubs.Hubs
{
    [Authorize] // in dev, your DevAuthHandler will make you "Admin" automatically
    public class CrowdCalendarHub : Hub
    {
        private readonly ILogger<CrowdCalendarHub> _logger;

        public CrowdCalendarHub(ILogger<CrowdCalendarHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("CrowdCalendar: client connected: {ConnId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            _logger.LogInformation("CrowdCalendar: client disconnected: {ConnId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        // ---- Subscriptions by region ----
        public Task JoinRegion(string regionCode)
        {
            if (string.IsNullOrWhiteSpace(regionCode)) return Task.CompletedTask;
            var group = CrowdCalendarHubMethods.RegionGroup(regionCode);
            _logger.LogDebug("JoinRegion {ConnId} -> {Group}", Context.ConnectionId, group);
            return Groups.AddToGroupAsync(Context.ConnectionId, group);
        }

        public Task LeaveRegion(string regionCode)
        {
            if (string.IsNullOrWhiteSpace(regionCode)) return Task.CompletedTask;
            var group = CrowdCalendarHubMethods.RegionGroup(regionCode);
            _logger.LogDebug("LeaveRegion {ConnId} -> {Group}", Context.ConnectionId, group);
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        }

        // ---- Subscriptions by location ----
        public Task JoinPlace(int placeId)
        {
            var group = CrowdCalendarHubMethods.PlaceGroup(placeId);
            _logger.LogDebug("JoinPlace {ConnId} -> {Group}", Context.ConnectionId, group);
            return Groups.AddToGroupAsync(Context.ConnectionId, group);
        }

        public Task LeavePlace(int placeId)
        {
            var group = CrowdCalendarHubMethods.PlaceGroup(placeId);
            _logger.LogDebug("LeavePlace {ConnId} -> {Group}", Context.ConnectionId, group);
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        }

        // Ping/debug (optional)
        public Task Ping() => Clients.Caller.SendAsync(CrowdCalendarHubMethods.ReceiveCalendarUpdated, new { ok = true });
    }
}

























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.