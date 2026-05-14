using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;

namespace CitizenHackathon2025.Infrastructure.Services;

public sealed class AntennaCadastreImportService : IAntennaCadastreImportService
{
    private readonly HttpClient _http;
    private readonly ICrowdInfoAntennaRepository _repository;
    private readonly AntennaCadastreOptions _options;
    private readonly ILogger<AntennaCadastreImportService> _logger;

    public AntennaCadastreImportService(
        HttpClient http,
        ICrowdInfoAntennaRepository repository,
        IOptions<AntennaCadastreOptions> options,
        ILogger<AntennaCadastreImportService> logger)
    {
        _http = http;
        _repository = repository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> ImportAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("Antenna cadastre import is disabled.");
            return 0;
        }

        if (string.IsNullOrWhiteSpace(_options.BaseQueryUrl))
            throw new InvalidOperationException("AntennaCadastre:BaseQueryUrl is missing.");

        var imported = 0;
        var offset = 0;
        var pageSize = Math.Clamp(_options.PageSize, 1, 2000);

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var url = BuildQueryUrl(offset, pageSize);

            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (!doc.RootElement.TryGetProperty("features", out var features) ||
                features.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Invalid GeoJSON response: missing features array.");
            }

            var countThisPage = 0;

            foreach (var feature in features.EnumerateArray())
            {
                var antenna = MapFeature(feature);

                if (antenna is null)
                    continue;

                await _repository.UpsertFromCadastreAsync(antenna, ct);

                imported++;
                countThisPage++;
            }

            _logger.LogInformation(
                "Imported antenna cadastre page. Offset={Offset}, Count={Count}",
                offset,
                countThisPage);

            if (countThisPage < pageSize)
                break;

            offset += pageSize;
        }

        _logger.LogInformation("Antenna cadastre import completed. Imported={Imported}", imported);

        return imported;
    }

    private string BuildQueryUrl(int offset, int pageSize)
    {
        var separator = _options.BaseQueryUrl.Contains('?') ? "&" : "?";

        return _options.BaseQueryUrl
            + separator
            + "where=1%3D1"
            + "&outFields=*"
            + "&outSR=4326"
            + "&f=geojson"
            + "&returnGeometry=true"
            + "&resultOffset=" + offset.ToString(CultureInfo.InvariantCulture)
            + "&resultRecordCount=" + pageSize.ToString(CultureInfo.InvariantCulture);
    }

    private CrowdInfoAntenna? MapFeature(JsonElement feature)
    {
        if (!feature.TryGetProperty("geometry", out var geometry))
            return null;

        if (!geometry.TryGetProperty("coordinates", out var coordinates))
            return null;

        if (coordinates.ValueKind != JsonValueKind.Array || coordinates.GetArrayLength() < 2)
            return null;

        var longitude = coordinates[0].GetDouble();
        var latitude = coordinates[1].GetDouble();

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            return null;

        feature.TryGetProperty("properties", out var props);

        var externalId =
            GetString(props, "OBJECTID")
            ?? GetString(props, "OBJECTID_1")
            ?? GetString(props, "ID")
            ?? GetString(props, "FID")
            ?? $"{Math.Round(latitude, 6).ToString(CultureInfo.InvariantCulture)}_{Math.Round(longitude, 6).ToString(CultureInfo.InvariantCulture)}";

        var name =
            GetString(props, "NOM")
            ?? GetString(props, "NAME")
            ?? GetString(props, "ADRESSE")
            ?? GetString(props, "COMMUNE")
            ?? $"Antenne Wallonie {externalId}";

        var commune = GetString(props, "COMMUNE");
        var adresse = GetString(props, "ADRESSE");
        var description = BuildDescription(commune, adresse);

        return new CrowdInfoAntenna
        {
            ExternalSource = _options.ExternalSource,
            ExternalId = externalId,
            Name = Trim(name, 64),
            Latitude = latitude,
            Longitude = longitude,
            Description = Trim(description, 256),
            MaxCapacity = null,
            Active = true
        };
    }

    private static string? BuildDescription(string? commune, string? adresse)
    {
        var parts = new[] { adresse, commune }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct()
            .ToArray();

        return parts.Length == 0
            ? "Cadastre des antennes émettrices stationnaires de Wallonie"
            : string.Join(" - ", parts);
    }

    private static string? GetString(JsonElement props, string name)
    {
        if (props.ValueKind != JsonValueKind.Object)
            return null;

        if (!props.TryGetProperty(name, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()) ? null : value.GetString(),
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static string? Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}

































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.