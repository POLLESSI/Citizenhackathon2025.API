namespace CitizenHackathon2025.DTOs.DTOs.Antennas
{
    public interface IAntennaTelemetryAggregator
    {
        void RecordPing(int antennaId, DateTime utcNow);

        /// <summary>
        /// Récupère et “détache” les fenêtres terminées (ex: la fenêtre précédente).
        /// </summary>
        IReadOnlyList<AntennaSnapshotRow> DequeueCompletedWindows(DateTime utcNow);

        public sealed record AntennaSnapshotRow(
            int AntennaId,
            DateTime WindowStartUtc,
            short WindowSeconds,
            int ActiveConnections,
            byte Confidence,
            byte Source
        );
    }
}


















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.