namespace CitizenHackathon2025.Shared.StaticConfig.Constants
{
    /// <summary>
    /// Names and paths used by the CrowdHub (SignalR).
    /// Single reference on server side (Hubs) and client side (Blazor/TS) to avoid "magic strings".
    /// </summary>
    public static class CrowdHubMethods
    {
        /// <summary>
        /// Hub path (must match API-side mapping: app.MapHub<CrowdHub>("/hubs/crowdHub"))
        /// </summary>
        public const string HubPath = "/hubs/crowdHub";
        /// <summary>
        /// Calls made by the server to clients (Clients.SendAsync(...))
        /// </summary>
        public static class ToClient
        {
            /// <summary>
            /// Broadcasting a new crowd info (payload string/JSON).
            /// Corresponds to: Clients.All.SendAsync("NewCrowdInfo", message)
            /// </summary>
            public const string NewCrowdInfo = "NewCrowdInfo";
            /// <summary>
            /// Generic notification / message (payload string).
            /// Corresponds to: Clients.All.SendAsync("ReceiveCrowdUpdate", message)
            /// </summary>
            public const string ReceiveCrowdUpdate = "ReceiveCrowdUpdate";
            /// <summary>
            /// Notification that crowd info has been archived (no payload).
            /// Corresponds to: Clients.All.SendAsync("CrowdInfoArchived")
            /// </summary>
            public const string CrowdInfoArchived = "CrowdInfoArchived";

            public const string CrowdRefreshRequested = "CrowdRefreshRequested";
        }

        /// <summary>
        /// Calls made by clients to the server (hubConnection.InvokeAsync(...))
        /// </summary>
        public static class FromClient
        {
            /// <summary>
            /// Asks the server to push a refresh notification.
            /// Hub signature: Task RefreshCrowdInfo(string message)
            /// </summary>
            public const string RefreshCrowdInfo = "RefreshCrowdInfo";
        }
    }
}
































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.