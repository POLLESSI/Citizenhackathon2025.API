namespace CitizenHackathon2025.Shared.StaticConfig.Constants
{
    /// <summary>
    /// Shared names and paths for PlaceHub (SignalR).
    /// To be referenced on both server and client side to avoid literal strings.
    /// </summary>
    public static class PlaceHubMethods
    {
        /// <summary>
        /// Hub path (must match MapHub on API side).
        /// </summary>
        public const string HubPath = "/hubs/placeHub";

        /// <summary>
        /// Events sent by the server to clients.
        /// </summary>
        public static class ToClient
        {
            /// <summary>
            /// Notification of new location/update.
            /// NB: respect the existing case "Newplace" in your current hub.
            /// </summary>
            public const string NewPlace = "Newplace";
        }

        /// <summary>
        /// Methods that clients invoke on the hub.
        /// </summary>
        public static class FromClient
        {
            /// <summary>
            /// Asks the server to broadcast a location update.
            /// Hub signature: Task RefreshPlace(string message)
            /// </summary>
            public const string RefreshPlace = "RefreshPlace";
        }
    }
}







































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.