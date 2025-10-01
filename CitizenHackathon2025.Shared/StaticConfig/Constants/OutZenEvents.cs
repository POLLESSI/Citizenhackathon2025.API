// CitizenHackathon2025.Shared/StaticConfig/Constants/OutZenEvents.cs
namespace CitizenHackathon2025.Shared.StaticConfig.Constants
{
    /// <summary>
    /// Events published by OutZenHub (typed hub IOutZenClient),
    /// exposed here as strings for consumption from JS/TS.
    /// </summary>
    public static class OutZenEvents
    {
        /// <summary>Full hub path (MapGroup "/hubs" + MapHub "/outzen").</summary>
        public const string HubPath = "/hubs/outzen";

        /// <summary>Group agreements (customers joining an event).</summary>
        public static class Groups
        {
            public const string EventPrefix = "event-";
            public static string BuildEventGroup(string eventId) => $"{EventPrefix}{eventId}";
        }

        /// <summary>Name of client-side methods (equal to IOutZenClient methods).</summary>
        public static class ToClient
        {
            /// <summary>Sending a new targeted suggestion.</summary>
            public const string NewSuggestion = "NewSuggestion";

            /// <summary>Update of a CrowdInfo (payload: DTO/object).</summary>
            public const string CrowdInfoUpdated = "CrowdInfoUpdated";

            /// <summary>Updated a list of suggestions.</summary>
            public const string SuggestionsUpdated = "SuggestionsUpdated";

            /// <summary>Weather updated.</summary>
            public const string WeatherUpdated = "WeatherUpdated";

            /// <summary>Traffic updated.</summary>
            public const string TrafficUpdated = "TrafficUpdated";
        }
    }
}