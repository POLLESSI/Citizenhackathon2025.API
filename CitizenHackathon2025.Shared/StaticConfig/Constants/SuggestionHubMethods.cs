// CitizenHackathon2025.Shared.StaticConfig.Constants.suggestionHubMethods.cs

namespace CitizenHackathon2025.Shared.StaticConfig.Constants
{
    /// <summary>Shared constants for SuggestionHub (SignalR).</summary>
    public static class SuggestionHubMethods
    {
        /// <summary>Hub path (must match the MapHub on the API side).</summary>
        public const string HubPath = "/hubs/suggestionHub";

        /// <summary>Events sent by the server to clients.</summary>
        public static class ToClient
        {
            /// <summary>Refresh ping (no payload).</summary>
            public const string NewSuggestion = "NewSuggestion";

            /// <summary>Suggestion sent (SuggestionDTO payload).</summary>
            public const string ReceiveSuggestion = "ReceiveSuggestion";
        }

        /// <summary>Methods invoked by clients on the hub.</summary>
        public static class FromClient
        {
            /// <summary>Ping request (no arguments).</summary>
            public const string RefreshSuggestion = "RefreshSuggestion";

            /// <summary>Sending a suggestion (SuggestionDTO payload).</summary>
            public const string SendSuggestion = "SendSuggestion";
        }
    }
}