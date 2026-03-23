using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Models;
using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class WallonieEnPocheSyncService : IWallonieEnPocheSyncService
    {
        private const string ExternalSourceName = "WallonieEnPoche";

        private readonly IWallonieEnPocheSourceClient _sourceClient;
        private readonly IWallonieEnPocheSyncRepository _syncRepository;
        private readonly IHubContext<PlaceHub> _placeHub;
        private readonly IHubContext<EventHub> _eventHub;
        private readonly ILogger<WallonieEnPocheSyncService> _logger;

        public WallonieEnPocheSyncService(
            IWallonieEnPocheSourceClient sourceClient,
            IWallonieEnPocheSyncRepository syncRepository,
            IHubContext<PlaceHub> placeHub,
            IHubContext<EventHub> eventHub,
            ILogger<WallonieEnPocheSyncService> logger)
        {
            _sourceClient = sourceClient;
            _syncRepository = syncRepository;
            _placeHub = placeHub;
            _eventHub = eventHub;
            _logger = logger;
        }

        public async Task<SyncReport> SyncAsync(CancellationToken ct = default)
        {
            var report = new SyncReport
            {
                StartedAtUtc = DateTime.UtcNow,
                Source = ExternalSourceName,
                Mode = _sourceClient.GetType().Name
            };

            try
            {
                var places = await _sourceClient.GetPlacesAsync(ct);
                report.PlacesFetched = places.Count;

                foreach (var place in places)
                {
                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        if (!IsValid(place))
                        {
                            report.PlacesSkipped++;
                            continue;
                        }

                        var normalized = Normalize(place);
                        var result = await _syncRepository.UpsertPlaceAsync(normalized, ExternalSourceName, ct);

                        if (result.Inserted)
                        {
                            report.PlacesInserted++;
                            await PublishPlaceCreatedAsync(result.Entity, ct);
                        }
                        else if (result.Updated)
                        {
                            report.PlacesUpdated++;
                            await PublishPlaceUpdatedAsync(result.Entity, ct);
                        }
                        else
                        {
                            report.PlacesSkipped++;
                        }
                    }
                    catch (Exception ex)
                    {
                        report.Errors++;
                        report.ErrorMessages.Add($"Place '{place.ExternalId}': {ex.Message}");
                        _logger.LogError(ex, "Error syncing place {ExternalId}", place.ExternalId);
                    }
                }

                var events = await _sourceClient.GetEventsAsync(ct);
                report.EventsFetched = events.Count;

                foreach (var ev in events)
                {
                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        if (!IsValid(ev))
                        {
                            report.EventsSkipped++;
                            continue;
                        }

                        var normalized = Normalize(ev);
                        var result = await _syncRepository.UpsertEventAsync(normalized, ExternalSourceName, ct);

                        if (result.Inserted)
                        {
                            report.EventsInserted++;
                            await PublishEventCreatedAsync(result.Entity, ct);
                        }
                        else if (result.Updated)
                        {
                            report.EventsUpdated++;
                            await PublishEventUpdatedAsync(result.Entity, ct);
                        }
                        else
                        {
                            report.EventsSkipped++;
                        }
                    }
                    catch (Exception ex)
                    {
                        report.Errors++;
                        report.ErrorMessages.Add($"Event '{ev.ExternalId}': {ex.Message}");
                        _logger.LogError(ex, "Error syncing event {ExternalId}", ev.ExternalId);
                    }
                }
            }
            finally
            {
                report.CompletedAtUtc = DateTime.UtcNow;
            }

            return report;
        }

        private static bool IsValid(WepPlaceImportDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ExternalId)) return false;
            if (string.IsNullOrWhiteSpace(dto.Name)) return false;
            if (dto.Latitude is null || dto.Longitude is null) return false;
            if (dto.Latitude < -90 || dto.Latitude > 90) return false;
            if (dto.Longitude < -180 || dto.Longitude > 180) return false;
            return true;
        }

        private static bool IsValid(WepEventImportDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ExternalId)) return false;
            if (string.IsNullOrWhiteSpace(dto.Name)) return false;
            if (dto.Latitude is null || dto.Longitude is null) return false;
            if (dto.Latitude < -90 || dto.Latitude > 90) return false;
            if (dto.Longitude < -180 || dto.Longitude > 180) return false;
            if (dto.DateEvent == default) return false;
            return true;
        }

        private static WepPlaceImportDTO Normalize(WepPlaceImportDTO dto)
        {
            return new WepPlaceImportDTO
            {
                ExternalId = dto.ExternalId.Trim(),
                Name = dto.Name.Trim(),
                Type = dto.Type?.Trim(),
                Indoor = dto.Indoor ?? false,
                Latitude = dto.Latitude.HasValue ? Math.Round(dto.Latitude.Value, 6) : null,
                Longitude = dto.Longitude.HasValue ? Math.Round(dto.Longitude.Value, 6) : null,
                Capacity = dto.Capacity ?? 0,
                Tag = dto.Tag?.Trim(),
                IsActive = dto.IsActive,
                SourceUpdatedAtUtc = dto.SourceUpdatedAtUtc?.ToUniversalTime()
            };
        }

        private static WepEventImportDTO Normalize(WepEventImportDTO dto)
        {
            return new WepEventImportDTO
            {
                ExternalId = dto.ExternalId.Trim(),
                PlaceExternalId = dto.PlaceExternalId?.Trim(),
                Name = dto.Name.Trim(),
                Latitude = dto.Latitude.HasValue ? Math.Round(dto.Latitude.Value, 6) : null,
                Longitude = dto.Longitude.HasValue ? Math.Round(dto.Longitude.Value, 6) : null,
                DateEvent = dto.DateEvent.Kind == DateTimeKind.Utc
                    ? dto.DateEvent
                    : DateTime.SpecifyKind(dto.DateEvent, DateTimeKind.Utc),
                ExpectedCrowd = dto.ExpectedCrowd,
                IsOutdoor = dto.IsOutdoor ?? false,
                IsActive = dto.IsActive,
                SourceUpdatedAtUtc = dto.SourceUpdatedAtUtc?.ToUniversalTime()
            };
        }

        private async Task PublishPlaceCreatedAsync(Place entity, CancellationToken ct)
        {
            await _placeHub.Clients.All.SendAsync("PlaceCreated", entity, ct);
        }

        private async Task PublishPlaceUpdatedAsync(Place entity, CancellationToken ct)
        {
            await _placeHub.Clients.All.SendAsync("PlaceUpdated", entity, ct);
        }

        private async Task PublishEventCreatedAsync(Event entity, CancellationToken ct)
        {
            await _eventHub.Clients.All.SendAsync("EventCreated", entity, ct);
        }

        private async Task PublishEventUpdatedAsync(Event entity, CancellationToken ct)
        {
            await _eventHub.Clients.All.SendAsync("EventUpdated", entity, ct);
        }
    }
}