// CitizenHackathon2025.Shared/StaticConfig/Constants/OutZenHubMethods.cs
namespace CitizenHackathon2025.Shared.StaticConfig.Constants
{
    /// <summary>Constants/conventions for OutZenHub (typed hub).</summary>
    public static class OutZenHubMethods
    {
        /// <summary>
        /// Full path. With MapGroup("/hubs") + MapHub("/outzen") => "/hubs/outzen".
        /// </summary>
        public const string HubPath = "/hubs/outzen";

        /// <summary>Group agreements.</summary>
        public static class Groups
        {
            public const string EventPrefix = "event-";

            public static string BuildEventGroup(string eventId) => $"{EventPrefix}{eventId}";
        }
    }
}






















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.