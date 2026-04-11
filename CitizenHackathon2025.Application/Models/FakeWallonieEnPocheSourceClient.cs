using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.DTOs;

namespace CitizenHackathon2025.Application.Models
{
    public sealed class FakeWallonieEnPocheSourceClient : IWallonieEnPocheSourceClient
    {
        public Task<IReadOnlyList<WepPlaceImportDTO>> GetPlacesAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<WepPlaceImportDTO>>(new[]
            {
            new WepPlaceImportDTO
            {
                ExternalId = "place-1001",
                Name = "Maison de famille",
                Type = "Culture",
                Indoor = true,
                Latitude = 50.467388m,
                Longitude = 4.871985m,
                Capacity = 120,
                Tag = "culture",
                IsActive = true,
                SourceUpdatedAtUtc = DateTime.UtcNow
            }
            });

        public Task<IReadOnlyList<WepEventImportDTO>> GetEventsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<WepEventImportDTO>>(new[]
            {
            new WepEventImportDTO
            {
                ExternalId = "event-9001",
                PlaceExternalId = "place-1001",
                Name = "Local Events - Family Home",
                Latitude = 50.467388m,
                Longitude = 4.871985m,
                DateEvent = DateTime.UtcNow.AddDays(3),
                ExpectedCrowd = 80,
                IsOutdoor = false,
                IsActive = true,
                SourceUpdatedAtUtc = DateTime.UtcNow
            }
            });
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.