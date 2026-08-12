using Azure.Core.GeoJson;

namespace CitizenHackathon2025.Domain.Entities
{
    public sealed class BeAlertCapAlert
    {
        public string Identifier { get; set; } = "";
        public string Sender { get; set; } = "";

        public DateTimeOffset Sent { get; set; }

        public string Status { get; set; } = "";
        public string MessageType { get; set; } = "";
        public string Scope { get; set; } = "";

        public string? Event { get; set; }

        public string? Urgency { get; set; }
        public string? Severity { get; set; }
        public string? Certainty { get; set; }

        public string? Headline { get; set; }
        public string? Description { get; set; }
        public string? Instruction { get; set; }

        public DateTimeOffset? Effective { get; set; }
        public DateTimeOffset? Expires { get; set; }

        public string? AreaDescription { get; set; }

        public IReadOnlyList<GeoPolygon> Polygons { get; set; }
            = [];

        //public IReadOnlyList<GeoCircle> Circles { get; set; }
        //    = [];

        public string? References { get; set; }
    }
}
