namespace CitizenHackathon2025.EmergencyIntelligence.Sources.BeAlert
{
    internal sealed class BeAlertCapMessage
    {
        public string Identifier { get; init; } = "";
        public string Sender { get; init; } = "";
        public DateTimeOffset Sent { get; init; }

        public string Status { get; init; } = "";
        public string MessageType { get; init; } = "";
        public string Scope { get; init; } = "";

        public string? References { get; init; }

        public string? Language { get; init; }

        public string? Event { get; init; }
        public string? Urgency { get; init; }
        public string? Severity { get; init; }
        public string? Certainty { get; init; }

        public string? Headline { get; init; }
        public string? Description { get; init; }
        public string? Instruction { get; init; }

        public DateTimeOffset? Effective { get; init; }
        public DateTimeOffset? Expires { get; init; }

        public List<BeAlertCapArea> Areas { get; init; } = [];
        public string RawXml { get; init; } = "";
    }


    internal sealed class BeAlertCapArea
    {
        public string AreaDescription { get; init; } = "";
        public List<string> Polygons { get; init; } = [];
        public List<string> Circles { get; init; } = [];
    }
}






































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.