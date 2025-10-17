namespace CitizenHackathon2025.Shared.StaticConfig.Constants
{
    public static class CrowdCalendarHubMethods
    {
        // Hub path (used in Program.cs)
        public const string HubPath = "/hubs/crowd-calendar";

        // Client-side methods (Receive*)
        public const string ReceiveAdvisory = "ReceiveAdvisory";                 // payload: an opinion
        public const string ReceiveAdvisories = "ReceiveAdvisories";             // payload: review list
        public const string ReceiveCalendarUpdated = "ReceiveCalendarUpdated";   // payload: simple metadata / ping

        // Groups (subscriptions)
        public static string RegionGroup(string regionCode) =>
            $"region:{regionCode?.Trim().ToUpperInvariant()}";

        public static string PlaceGroup(int placeId) =>
            $"place:{placeId}";
    }
}






























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.