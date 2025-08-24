using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Hubs.Hubs
{
    public class OutZenHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var eventId = Context.GetHttpContext()?.Items["OutZen.EventId"] as string;

            if (!string.IsNullOrEmpty(eventId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"event-{eventId}");
                Console.WriteLine($"[OutZenHub] Client {Context.ConnectionId} joined group event-{eventId}");
            }
            else
            {
                Console.WriteLine($"[OutZenHub] Client {Context.ConnectionId} connected without EventId");
                // Optional: Disconnect if you want to force the presence of an event
                // await Context.Abort();
            }

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
        public async Task SendCrowdInfo(string eventId, CrowdInfoDTO dto)
            => await Clients.Group($"event-{eventId}").SendAsync("CrowdInfoUpdated", dto);

        public async Task SendSuggestions(string eventId, List<SuggestionDTO> suggestions)
            => await Clients.Group($"event-{eventId}").SendAsync("SuggestionsUpdated", suggestions);
    }
}































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.