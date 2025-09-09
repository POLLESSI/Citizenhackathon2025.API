using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Hubs.Hubs
{
    [Authorize(Policy = "User")]
    public class OutZenHub : Hub<IOutZenClient>
    {
        public override async Task OnConnectedAsync()
        {
            var http = Context.GetHttpContext();
            var eventId = http?.Items["OutZen.EventId"]?.ToString();
            if (!string.IsNullOrEmpty(eventId))
                await Groups.AddToGroupAsync(Context.ConnectionId, $"event-{eventId}");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"[OutZenHub] Client {Context.ConnectionId} disconnected. Reason: {exception?.Message}");
            await base.OnDisconnectedAsync(exception);
        }

        // ✅ Join / Leave explicit
        public async Task JoinEventGroup(string eventId)
        {
            if (!string.IsNullOrEmpty(eventId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"event-{eventId}");
                Console.WriteLine($"[OutZenHub] {Context.ConnectionId} joined event-{eventId}");
            }
        }

        public async Task LeaveEventGroup(string eventId)
        {
            if (!string.IsNullOrEmpty(eventId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"event-{eventId}");
                Console.WriteLine($"[OutZenHub] {Context.ConnectionId} left event-{eventId}");
            }
        }

        // ✅ Broadcast targeted at the event
        //public async Task SendCrowdInfo(string eventId, CrowdInfoDTO dto)
        //    => await Clients.Group($"event-{eventId}").SendAsync("CrowdInfoUpdated", dto);

        //public async Task SendSuggestions(string eventId, List<SuggestionDTO> suggestions)
        //    => await Clients.Group($"event-{eventId}").SendAsync("SuggestionsUpdated", suggestions);
    }
}































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.