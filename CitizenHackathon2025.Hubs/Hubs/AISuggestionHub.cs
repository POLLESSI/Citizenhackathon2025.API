using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using CitizenHackathon2025.Contracts.Hubs;

namespace CitizenHackathon2025.Hubs.Hubs
{
    /// <summary>
    /// Hub for travel suggestions/AI.
    /// Use TourismeHubMethods for names/paths.
    /// </summary>
    public class AISuggestionHub : Hub
    {
        private readonly ILogger<AISuggestionHub> _logger;

        public AISuggestionHub(ILogger<AISuggestionHub> logger)
        {
            _logger = logger;
        }

        /// <summary>Join a logical group (eg city, event, map).</summary>
        public Task JoinGroup(string group)
        {
            if (!string.IsNullOrWhiteSpace(group))
            {
                _logger.LogInformation("[AISuggestionHub] {Conn} joined {Group}", Context.ConnectionId, group);
                return Groups.AddToGroupAsync(Context.ConnectionId, group);
            }
            return Task.CompletedTask;
        }

        /// <summary>Leave a logical group.</summary>
        public Task LeaveGroup(string group)
        {
            if (!string.IsNullOrWhiteSpace(group))
            {
                _logger.LogInformation("[AISuggestionHub] {Conn} left {Group}", Context.ConnectionId, group);
                return Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Client-side request to calculate suggestions (payload: prompt).
        /// You can connect this call to your AI service via MediatR/handler if you want.
        /// Here, by default, we simply relay a "request" to other clients/admins.
        /// </summary>
        public Task RequestSuggestions(string prompt)
        {
            _logger.LogInformation("[AISuggestionHub] RequestSuggestions: {Prompt}", prompt);
            // Option: notify a backoffice or admin page
            return Clients.All.SendAsync(TourismeHubMethods.FromClient.RequestSuggestions, prompt);
        }

        /// <summary>
        /// Broadcast suggestions to all clients (JSON/DTO payload already ready).
        /// </summary>
        public Task BroadcastSuggestions(string payloadJson)
        {
            _logger.LogInformation("[AISuggestionHub] BroadcastSuggestions ({Length} bytes)", payloadJson?.Length ?? 0);
            return Clients.All.SendAsync(TourismeHubMethods.ToClient.SuggestionsUpdated, payloadJson);
        }

        /// <summary>Broadcast to a group (eg: "city-brussels").</summary>
        public Task BroadcastSuggestionsToGroup(string group, string payloadJson)
        {
            _logger.LogInformation("[AISuggestionHub] Broadcast to {Group}", group);
            return Clients.Group(group).SendAsync(TourismeHubMethods.ToClient.SuggestionsUpdated, payloadJson);
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("[AISuggestionHub] Connected: {Conn}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(System.Exception? exception)
        {
            _logger.LogInformation("[AISuggestionHub] Disconnected: {Conn} ({Err})", Context.ConnectionId, exception?.Message);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.