namespace CitizenHackathon2025.Shared.StaticConfig.Constants
{
    /// <summary>
    /// Shared constants (server + client) for the UserHub (SignalR).
    /// Avoids string literals and makes refactors easier.
    /// </summary>
    public static class UserHubMethods
    {
        /// <summary>Hub path. Must match the MapHub on the API side.</summary>
        public const string HubPath = "/hubs/userHub";

        /// <summary>Events sent by the server to clients.</summary>
        public static class ToClient
        {
            /// <summary>
            /// Notification that a user has just been registered.
            /// Corresponds to: Clients.All.SendAsync("UserRegistered", email)
            /// </summary>
            public const string UserRegistered = "UserRegistered";
        }

        /// <summary>Methods invoked by clients to the server.</summary>
        public static class FromClient
        {
            /// <summary>
            /// Request to issue a registration notification.
            /// Signature hub: Task NotifyUserRegistered(string email)
            /// </summary>
            public const string NotifyUserRegistered = "NotifyUserRegistered";
        }
    }
}
