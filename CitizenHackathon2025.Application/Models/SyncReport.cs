namespace CitizenHackathon2025.Application.Models
{
    /// <summary>
    /// Detailed result of an external synchronization (Wallonia in Your Pocket)
    /// </summary>
    public sealed class SyncReport
    {
        public DateTime StartedAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime? CompletedAtUtc { get; set; }

        // === Places ===
        public int PlacesFetched { get; set; }
        public int PlacesInserted { get; set; }
        public int PlacesUpdated { get; set; }
        public int PlacesSkipped { get; set; }

        // === Events ===
        public int EventsFetched { get; set; }
        public int EventsInserted { get; set; }
        public int EventsUpdated { get; set; }
        public int EventsSkipped { get; set; }

        // === Errors ===
        public int Errors { get; set; }
        public List<string> ErrorMessages { get; set; } = new();

        // === Diagnostic ===
        public string Source { get; set; } = "WallonieEnPoche";
        public string Mode { get; set; } = "Unknown"; // Fake / API / SQL / JSON

        public TimeSpan Duration => CompletedAtUtc.HasValue ? CompletedAtUtc.Value - StartedAtUtc : TimeSpan.Zero;
    }
}