using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.DTOs.GeoPortal;
using CitizenHackathon2025.Infrastructure.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class GeoPortalFeedService : IGeoPortalFeedService
    {
        private const string FreshCacheKey = "outzen:geoportal:feeds:fresh";

        private const string StaleCacheKey = "outzen:geoportal:feeds:stale";

        private static readonly Regex DateRegex =
            new(
                @"(?<!\d)(\d{2}/\d{2}/\d{4})(?!\d)",
                RegexOptions.Compiled |
                RegexOptions.CultureInvariant
            );

        private static readonly Regex HtmlTagRegex =
            new(
                @"<[^>]+>",
                RegexOptions.Compiled |
                RegexOptions.CultureInvariant
            );

        private static readonly Regex WhiteSpaceRegex =
            new(
                @"\s+",
                RegexOptions.Compiled |
                RegexOptions.CultureInvariant
            );

        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly GeoPortalFeedOptions _options;
        private readonly ILogger<GeoPortalFeedService> _logger;

        public GeoPortalFeedService(HttpClient httpClient, IMemoryCache cache, IOptions<GeoPortalFeedOptions> options, ILogger<GeoPortalFeedService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _options = options.Value;
            _logger = logger;
        }


        public Task<GeoPortalFeedSnapshotDto> GetAsync(CancellationToken cancellationToken = default)
        {
            return GetInternalAsync(forceRefresh: false, cancellationToken
            );
        }


        public Task<GeoPortalFeedSnapshotDto> RefreshAsync(CancellationToken cancellationToken = default)
        {
            return GetInternalAsync(forceRefresh: true, cancellationToken
            );
        }


        private async Task<GeoPortalFeedSnapshotDto> GetInternalAsync(bool forceRefresh, CancellationToken cancellationToken)
        {
            if (!forceRefresh && _cache.TryGetValue(FreshCacheKey, out GeoPortalFeedSnapshotDto? fresh) && fresh is not null)
            {
                return fresh with
                {
                    FromCache = true
                };
            }

            _cache.TryGetValue(StaleCacheKey, out GeoPortalFeedSnapshotDto? stale);

            if (_options.Sources.Count == 0)
            {
                return new GeoPortalFeedSnapshotDto
                {
                    IsSuccess = false,
                    HasErrors = true,
                    GeneratedAtUtc = DateTimeOffset.UtcNow,
                    ErrorMessage = "Aucune source Géoportail n'est configurée."
                };
            }

            var tasks = _options.Sources
                .Select(source => FetchSourceAsync(source, cancellationToken))
                .ToArray();

            var results = await Task.WhenAll(tasks);
            var now = DateTimeOffset.UtcNow;
            var items = new List<GeoPortalFeedItemDto>();
            var sourceStates = new List<GeoPortalFeedSourceStatusDto>();

            var successfulSources = 0;
            var staleDataUsed = false; 

            foreach (var result in results)
            {
                if (result.Success)
                {
                    successfulSources++;

                    items.AddRange(result.Items);

                    sourceStates.Add(
                        new GeoPortalFeedSourceStatusDto
                        {
                            SourceCode = result.Source.Code,

                            SourceName = result.Source.Name,

                            IsSuccess = true,

                            IsStale = false,

                            ItemCount = result.Items.Count,

                            LastSuccessfulSyncUtc = now
                        }
                    );

                    continue;
                }

                var oldItems =
                    stale?.Items
                        .Where(x =>
                            string.Equals(
                                x.SourceCode,
                                result.Source.Code,
                                StringComparison.OrdinalIgnoreCase
                            ))
                        .ToArray()
                    ?? Array.Empty<GeoPortalFeedItemDto>();

                var oldSourceState =
                    stale?.Sources
                        .FirstOrDefault(x =>
                            string.Equals(
                                x.SourceCode,
                                result.Source.Code,
                                StringComparison.OrdinalIgnoreCase
                            ));

                if (oldItems.Length > 0)
                {
                    staleDataUsed = true;

                    items.AddRange(oldItems);
                }

                sourceStates.Add(
                    new GeoPortalFeedSourceStatusDto
                    {
                        SourceCode = result.Source.Code,
                        SourceName = result.Source.Name,
                        IsSuccess = false,
                        IsStale = oldItems.Length > 0,
                        ItemCount = oldItems.Length,
                        LastSuccessfulSyncUtc = oldSourceState ?.LastSuccessfulSyncUtc,
                        ErrorMessage = result.ErrorMessage
                    }
                );
            }

            items = items
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderByDescending(x => x.PublishedAtUtc ?? DateTimeOffset.MinValue)
                .ThenBy(x => x.EventStartDate ?? DateOnly.MaxValue)
                .ToList();

            var allSourcesSuccessful = successfulSources == _options.Sources.Count;

            var noSourceSuccessful = successfulSources == 0;

            DateTimeOffset? lastSuccessfulSyncUtc = sourceStates
                .Where(x => x.LastSuccessfulSyncUtc.HasValue)
                .Select(x => x.LastSuccessfulSyncUtc)
                .OrderByDescending(x => x)
                .FirstOrDefault();

            /*All the sources have dried up
             * ,
             * but we still possess some
             * previous data.
             */
            if (noSourceSuccessful && stale is not null && items.Count > 0)
            {
                return new GeoPortalFeedSnapshotDto
                {
                    IsSuccess = false,
                    HasErrors = true,
                    FromCache = true,
                    IsStale = true,
                    GeneratedAtUtc = now,
                    LastSuccessfulSyncUtc = stale.LastSuccessfulSyncUtc,
                    ErrorMessage = "The Geoportal is temporarily unavailable. " + "OutZen displays the last known copy.",
                    Items = items,
                    Sources = sourceStates
                };
            }

            var snapshot =
                new GeoPortalFeedSnapshotDto
                {
                    IsSuccess = successfulSources > 0,
                    HasErrors = !allSourcesSuccessful,
                    FromCache = false,
                    IsStale = staleDataUsed,
                    GeneratedAtUtc = now,
                    LastSuccessfulSyncUtc = lastSuccessfulSyncUtc,
                    ErrorMessage = allSourcesSuccessful ? null : "Une ou plusieurs sources Géoportail " + "n'ont pas pu être actualisées.",
                    Items = items,
                    Sources = sourceStates
                };

            /*
             * If everything goes well :
             *
             * - fresh cache 10 min
             * - stale cache 6 h
             */
            if (allSourcesSuccessful)
            {
                _cache.Set(FreshCacheKey, snapshot, TimeSpan.FromMinutes(Math.Max(1, _options.CacheMinutes)));
                _cache.Set(StaleCacheKey, snapshot, TimeSpan.FromHours(Math.Max(1, _options.StaleHours)));

                return snapshot;
            }

            /*
             * Partial failure :
             * very short cache to avoid
             * hammering the remote server.
             *
             * DO NOT replace the stale-cache
             * completely with an incomplete snapshot.
             */
            if (successfulSources > 0)
            {
                _cache.Set(FreshCacheKey, snapshot, TimeSpan.FromMinutes(Math.Max(1, _options.PartialFailureCacheMinutes)));
            }

            return snapshot;
        }


        private async Task<FeedFetchResult> FetchSourceAsync(GeoPortalFeedSourceOptions source, CancellationToken cancellationToken)
        {
            try
            {
                var uri = ValidateSourceUri(source.Url);

                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GeoPortal source {Source} returned HTTP {StatusCode}", source.Code, (int)response.StatusCode);

                    return FeedFetchResult.Failed(source, $"HTTP {(int)response.StatusCode}");
                }

                var declaredLength = response.Content.Headers.ContentLength;

                if (declaredLength.HasValue && declaredLength.Value > _options.MaxFeedBytes)
                {
                    _logger.LogWarning("GeoPortal source {Source} exceeded maximum declared size", source.Code);

                    return FeedFetchResult.Failed(source, "Flux trop volumineux.");
                }

                var bytes =
                    await ReadLimitedAsync(
                        response.Content,
                        _options.MaxFeedBytes,
                        cancellationToken
                    );

                var parsed =
                    await ParseFeedAsync(
                        bytes,
                        source,
                        cancellationToken
                    );

                _logger.LogInformation(
                    "GeoPortal source {Source} loaded: {Count} items",
                    source.Code,
                    parsed.Count
                );

                return FeedFetchResult.Succeeded(
                    source,
                    parsed
                );
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "GeoPortal source {Source} timed out",
                    source.Code
                );

                return FeedFetchResult.Failed(
                    source,
                    "Délai d'attente dépassé."
                );
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "GeoPortal HTTP error for {Source}",
                    source.Code
                );

                return FeedFetchResult.Failed(
                    source,
                    "Source distante indisponible."
                );
            }
            catch (XmlException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid GeoPortal XML for {Source}",
                    source.Code
                );

                return FeedFetchResult.Failed(
                    source,
                    "Le flux RSS reçu n'est pas un XML valide."
                );
            }
            catch (InvalidDataException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid GeoPortal payload for {Source}",
                    source.Code
                );

                return FeedFetchResult.Failed(
                    source,
                    "Flux distant refusé."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected GeoPortal error for {Source}",
                    source.Code
                );

                return FeedFetchResult.Failed(
                    source,
                    "Erreur inattendue lors de la synchronisation."
                );
            }
        }


        private async Task<XDocument> LoadFeedDocumentAsync(byte[] bytes, string sourceCode, CancellationToken cancellationToken)
        {
            try
            {
                return await LoadXmlStrictAsync(bytes, cancellationToken);
            }
            catch (XmlException firstException)
            {
                _logger.LogWarning(
                    firstException,
                    """
                        GeoPortal source {SourceCode} is not strict XML.
                        First parsing failure:
                        Message={Message}
                        Line={LineNumber}
                        Position={LinePosition}
                        PayloadBytes={PayloadBytes}
                        Trying RSS fragment recovery.
                        """,
                    sourceCode,
                    firstException.Message,
                    firstException.LineNumber,
                    firstException.LinePosition,
                    bytes.Length
                );

                var rssBytes = TryExtractRssFragment(bytes, sourceCode);

                if (rssBytes is null)
                {
                    _logger.LogError(
                        """
                            GeoPortal source {SourceCode} recovery failed:
                            no complete <rss>...</rss> fragment was found.
                            PayloadBytes={PayloadBytes}
                            """,
                        sourceCode,
                        bytes.Length
                    );

                    throw;
                }

                _logger.LogInformation(
                    """
                        GeoPortal source {SourceCode}:
                        RSS fragment extracted successfully.
                        FragmentBytes={FragmentBytes}
                        Retrying strict XML parsing.
                        """,
                    sourceCode,
                    rssBytes.Length
                );

                try
                {
                    return await LoadXmlStrictAsync(rssBytes, cancellationToken);
                }
                catch (XmlException recoveryException)
                {
                    _logger.LogError(
                        recoveryException,
                        """
                            GeoPortal source {SourceCode}:
                            extracted RSS fragment is still invalid XML.
                            Message={Message}
                            Line={LineNumber}
                            Position={LinePosition}
                            FragmentBytes={FragmentBytes}
                            """,
                        sourceCode,
                        recoveryException.Message,
                        recoveryException.LineNumber,
                        recoveryException.LinePosition,
                        rssBytes.Length
                    );

                    throw;
                }
            }
        }

        private async Task<XDocument> LoadXmlStrictAsync(
    byte[] bytes,
    CancellationToken cancellationToken)
        {
            await using var stream =
                new MemoryStream(
                    bytes,
                    writable: false
                );

            var settings =
                new XmlReaderSettings
                {
                    Async = true,

                    /*
                     * On conserve toutes les protections.
                     */
                    DtdProcessing =
                        DtdProcessing.Prohibit,

                    XmlResolver =
                        null,

                    IgnoreComments =
                        true,

                    IgnoreWhitespace =
                        true,

                    MaxCharactersInDocument =
                        Math.Max(
                            100_000,
                            (long)_options.MaxFeedBytes * 2
                        )
                };

            using var reader =
                XmlReader.Create(
                    stream,
                    settings
                );

            return await XDocument.LoadAsync(
                reader,
                LoadOptions.None,
                cancellationToken
            );
        }

        private byte[]? TryExtractRssFragment(byte[] bytes, string sourceCode)
        {
            if (bytes.Length == 0)
            {
                return null;
            }

            var text = Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');
            var rssStart = text.IndexOf("<rss", StringComparison.OrdinalIgnoreCase);

            if (rssStart < 0)
            {
                return null;
            }

            var rssEnd = text.IndexOf("</rss>", rssStart, StringComparison.OrdinalIgnoreCase);

            if (rssEnd < 0)
            {
                return null;
            }

            rssEnd += "</rss>".Length;

            var rssXml = text.Substring(rssStart, rssEnd - rssStart);

            if (!rssXml.StartsWith("<rss", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            rssXml = NormalizeGeoPortalRss(rssXml, sourceCode);

            return Encoding.UTF8.GetBytes(rssXml);
        }

        private string NormalizeGeoPortalRss(string rssXml, string sourceCode)
        {
            if (string.IsNullOrWhiteSpace(rssXml))
            {
                return rssXml;
            }

            var normalized = Regex.Replace(rssXml, @"<rss\s+ve\s+sion\s*=\s*", "<rss version=", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (!string.Equals( normalized, rssXml, StringComparison.Ordinal))
            {
                _logger.LogWarning("GeoPortal source {SourceCode}: " + "repaired malformed RSS version attribute.", sourceCode);
            }

            return normalized;
        }

        private async Task<IReadOnlyList<GeoPortalFeedItemDto>>ParseFeedAsync(byte[] bytes, GeoPortalFeedSourceOptions source, CancellationToken cancellationToken)
        {
            var document = await LoadFeedDocumentAsync(bytes, source.Code, cancellationToken);
            var rssItems = document
                    .Descendants()
                    .Where(x => string.Equals(x.Name.LocalName, "item", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

            var result = new List<GeoPortalFeedItemDto>(rssItems.Length);

            foreach (var item in rssItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var title = GetElementValue(item, "title")?.Trim();

                /*
                 * An untitled item cannot be used
                 * on the OutZen side.
                 */
                if (string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                var guid = GetElementValue(item, "guid");
                var link = NormalizeGeoPortalUrl(GetElementValue(item, "link"));
                var rawDescription = GetElementValue(item, "description");
                var summary = CleanDescription(rawDescription);
                var publication = ParsePublicationDate(GetElementValue(item, "pubDate"));
                var eventDates = ParseEventDates(summary);

                /*
                 * The RSS GUID is preferred.
                 *
                 * If it is absent:
                 *   GUID
                 *      ↓
                 *   URL
                 *      ↓
                 *   combination of source/title/date
                 */
                var identity = guid ?? link ?? $"{source.Code}|{title}|{publication:O}";

                result.Add(
                    new GeoPortalFeedItemDto
                    {
                        Id = BuildStableId(source.Code, identity),
                        SourceCode = source.Code,
                        SourceName = source.Name,
                        FeedKind = source.Kind,
                        Title = title,
                        Summary = summary,
                        Url = link,
                        PublishedAtUtc = publication,
                        EventStartDate = eventDates.Start,
                        EventEndDate = eventDates.End
                    }
                );
            }

            return result;
        }
        private static string? GetElementValue(XElement parent, string localName)
        {
            return parent
                .Elements()
                .FirstOrDefault(x => string.Equals(x.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase))
                ?.Value;
        }


        private static DateTimeOffset? ParsePublicationDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result))
            {
                return result;
            }
            return null;
        }


        private static (DateOnly? Start, DateOnly? End) ParseEventDates(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return (null, null);
            }

            var matches = DateRegex.Matches(description);

            if (matches.Count == 0)
            {
                return (null, null);
            }

            DateOnly? Parse(string value)
            {
                return DateOnly.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;
            }

            var start = Parse(matches[0].Groups[1].Value);
            var end = matches.Count >= 2 ? Parse(matches[1].Groups[1].Value) : start;

            return (start, end);
        }


        private static string? CleanDescription(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var decoded = WebUtility.HtmlDecode(raw);

            decoded = decoded.Replace("<br/>", " ", StringComparison.OrdinalIgnoreCase);
            decoded = decoded.Replace("<br />", " ", StringComparison.OrdinalIgnoreCase);
            decoded = HtmlTagRegex.Replace(decoded, " ");
            decoded = WebUtility.HtmlDecode(decoded);
            decoded = decoded.Replace('\u00A0', ' ');

            return WhiteSpaceRegex
                .Replace(decoded, " ")
                .Trim();
        }


        private static string BuildStableId(string sourceCode, string identity)
        {
            var bytes = Encoding.UTF8.GetBytes(identity);
            var hash = SHA256.HashData(bytes);
            var fingerprint = Convert.ToHexString(hash)
                    .Substring(0, 20);

            return $"{sourceCode}:{fingerprint}";
        }


        private static string? NormalizeGeoPortalUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                return null;
            }

            /*
             * In particular, we refuse :
             *
             * javascript:
             * data:
             * file:
             */
            if (uri.Scheme != Uri.UriSchemeHttps)
            {
                return null;
            }

            if (!string.Equals(uri.Host, "geoportail.wallonie.be", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return uri.ToString();
        }


        private static Uri ValidateSourceUri(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                throw new InvalidDataException("Invalid GeoPortal URI.");
            }

            /*
             * SSRF Protection :
             *
             * The service does NOT accept
             * any URL coming from
             * a user-provided parameter.
             */
            if (uri.Scheme != Uri.UriSchemeHttps || !string.Equals(uri.Host, "geoportail.wallonie.be", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("GeoPortal source is not allowed.");
            }

            return uri;
        }

        private static async Task<byte[]> ReadLimitedAsync(HttpContent content, int maximumBytes, CancellationToken cancellationToken)
        {
            await using var input = await content.ReadAsStreamAsync(cancellationToken);
            await using var output = new MemoryStream();

            var buffer = new byte[81920];

            var total = 0;

            while (true)
            {
                var read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read <= 0)
                {
                    break;
                }

                total += read;

                if (total > maximumBytes)
                {
                    throw new InvalidDataException("GeoPortal feed exceeded the configured size limit.");
                }
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
            return output.ToArray();
        }


        private sealed record FeedFetchResult(GeoPortalFeedSourceOptions Source, bool Success, IReadOnlyList<GeoPortalFeedItemDto> Items, string? ErrorMessage)
        {
            public static FeedFetchResult Succeeded(GeoPortalFeedSourceOptions source, IReadOnlyList<GeoPortalFeedItemDto> items)
            {
                return new FeedFetchResult(source, true, items, null);
            }


            public static FeedFetchResult Failed(GeoPortalFeedSourceOptions source, string errorMessage)
            {
                return new FeedFetchResult(source, false, Array.Empty<GeoPortalFeedItemDto>(), errorMessage);
            }
        }
    }
}

































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.