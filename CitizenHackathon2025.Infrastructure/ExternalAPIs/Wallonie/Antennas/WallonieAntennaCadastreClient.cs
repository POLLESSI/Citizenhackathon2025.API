using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Wallonie.Antennas;

public sealed class WallonieAntennaCadastreClient
{
    private readonly HttpClient _http;

    public WallonieAntennaCadastreClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<WallonieAntennaSiteDto>> GetSitesAsync(CancellationToken ct)
    {
        var result = new List<WallonieAntennaSiteDto>();

        var offset = 0;
        const int pageSize = 2000;

        while (true)
        {
            var url =
                "query" +
                "?where=1%3D1" +
                "&outFields=ID_SITE_SPW,LOCALITE,RUE,COMMUNE,NBRE_OPERATEUR,TYPE_IMPLANTATION" +
                "&returnGeometry=true" +
                "&outSR=4326" +
                "&f=json" +
                $"&resultOffset={offset}" +
                $"&resultRecordCount={pageSize}";

            var response = await _http.GetFromJsonAsync<ArcGisFeatureResponse>(url, ct);

            var features = response?.Features ?? [];

            foreach (var feature in features)
            {
                var a = feature.Attributes;

                var lat = feature.Geometry?.Y; // latitude
                var lon = feature.Geometry?.X; // longitude

                if (lat is null || lon is null)
                    continue;

                if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
                    continue;

                if (a.IdSiteSpw is null)
                    continue;

                result.Add(new WallonieAntennaSiteDto
                {
                    ExternalId = a.IdSiteSpw.Value.ToString(),
                    Localite = a.Localite,
                    Rue = a.Rue,
                    Commune = a.Commune,

                    Latitude = Math.Round(lat.Value, 6),
                    Longitude = Math.Round(lon.Value, 6),

                    OperatorCount = a.OperatorCount,
                    TypeImplantation = a.TypeImplantation
                });
            }

            if (features.Count < pageSize)
                break;

            offset += pageSize;
        }

        return result;
    }

    private sealed class ArcGisFeatureResponse
    {
        [JsonPropertyName("features")]
        public List<ArcGisFeature> Features { get; set; } = [];
    }

    private sealed class ArcGisFeature
    {
        [JsonPropertyName("attributes")]
        public ArcGisAttributes Attributes { get; set; } = new();

        [JsonPropertyName("geometry")]
        public ArcGisGeometry? Geometry { get; set; }
    }

    private sealed class ArcGisGeometry
    {
        [JsonPropertyName("x")]
        public double? X { get; set; } // longitude

        [JsonPropertyName("y")]
        public double? Y { get; set; } // latitude
    }

    private sealed class ArcGisAttributes
    {
        [JsonPropertyName("ID_SITE_SPW")]
        public int? IdSiteSpw { get; set; }

        [JsonPropertyName("LOCALITE")]
        public string? Localite { get; set; }

        [JsonPropertyName("RUE")]
        public string? Rue { get; set; }

        [JsonPropertyName("COMMUNE")]
        public string? Commune { get; set; }

        //[JsonPropertyName("LAT_ETRS89")]
        //public double? Lat { get; set; }

        //[JsonPropertyName("LON_ETRS89")]
        //public double? Lon { get; set; }

        [JsonPropertyName("NBRE_OPERATEUR")]
        public int? OperatorCount { get; set; }

        [JsonPropertyName("TYPE_IMPLANTATION")]
        public string? TypeImplantation { get; set; }
    }
}

public sealed class WallonieAntennaSiteDto
{
    public string ExternalId { get; set; } = "";
    public string? Localite { get; set; }
    public string? Rue { get; set; }
    public string? Commune { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int? OperatorCount { get; set; }
    public string? TypeImplantation { get; set; }
}

















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.