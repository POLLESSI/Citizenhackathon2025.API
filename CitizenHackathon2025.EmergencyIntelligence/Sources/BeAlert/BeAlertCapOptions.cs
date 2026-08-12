namespace CitizenHackathon2025.EmergencyIntelligence.Sources.BeAlert
{
    public sealed class BeAlertCapOptions
    {
        public const string SectionName = "EmergencyIntelligence:BeAlert";

        public bool Enabled { get; set; }

        public string FeedUrl { get; set; } = string.Empty;

        public string PreferredLanguage { get; set; } = "fr-BE";

        public int TimeoutSeconds { get; set; } = 15;
            
        public int MaxPayloadBytes { get; set; } = 2_000_000;
    }
}
























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.