// CitizenHackathon2025.Shared/StaticConfig/Constants/NotificationHubMethods.cs
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Microsoft.AspNetCore.SignalR;
using System;

namespace CitizenHackathon2025.Shared.StaticConfig.Constants
{
    /// <summary>
    /// Shared constants for NotificationHub (generic notifications).
    /// </summary>
    public static class NotificationHubMethods
    {
        /// <summary>
        /// Hub path (group /hubs + map "/notifications" => "/hubs/notifications").
        /// See Program.cs: hubs.MapHub<NotificationHub>("/notifications");
        /// </summary>
        public const string HubPath = "/hubs/notifications";

        /// <summary>Server -> Client Events.</summary>
        public static class ToClient
        {
            /// <summary>Generic notification (payload: string).</summary>
            public const string Notify = "Notify";

            /// <summary>Targeted user notification (payload: string userId/email, string message).</summary>
            public const string NotifyUser = "NotifyUser";

            /// <summary>System/maintenance notification (payload: string).</summary>
            public const string System = "NotifySystem";
        }

        /// <summary>Client -> server methods.</summary>
        public static class FromClient
        {
            /// <summary>Ping for test (payload: string message).</summary>
            public const string Ping = "Ping";

            /// <summary>Generic broadcast request (payload: string message).</summary>
            public const string Broadcast = "Broadcast";
        }
    }
}










































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.