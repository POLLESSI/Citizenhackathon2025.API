// CitizenHackathon2025.Shared/StaticConfig/Constants/UpdateHubMethods.cs
namespace CitizenHackathon2025.Shared.StaticConfig.Constants
{
    /// <summary>Shared constants for UpdateHub.</summary>
    public static class UpdateHubMethods
    {
        /// <summary>
        /// Full path to the hub. With your MapGroup("/hubs") + MapHub("/updateHub"),
        /// the final URL = "/hubs/updateHub".
        /// </summary>
        public const string HubPath = "/hubs/updateHub";

        /// <summary>Server -> client events (untyped Hub).</summary>
        public static class ToClient
        {
            /// <summary>Announcement of a new update available (payload: string version or JSON).</summary>
            public const string UpdateAvailable = "UpdateAvailable";

            /// <summary>Config change (payload: string key or JSON).</summary>
            public const string ConfigChanged = "ConfigChanged";

            /// <summary>Deployment/update progress (payload: int percentage or object).</summary>
            public const string UpdateProgress = "UpdateProgress";
        }

        /// <summary>Client -> server methods.</summary>
        public static class FromClient
        {
            /// <summary>The client asks if there is an update (recommended signature: Task CheckForUpdate()).</summary>
            public const string CheckForUpdate = "CheckForUpdate";

            /// <summary>The client acknowledges/consumes an update (signature: Task AckUpdate(string version)).</summary>
            public const string AckUpdate = "AckUpdate";
        }
    }
}
