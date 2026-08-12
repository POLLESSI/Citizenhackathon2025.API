using CitizenHackathon2025.EmergencyIntelligence.Records;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;

namespace CitizenHackathon2025.EmergencyIntelligence.Sources.BeAlert
{
    public sealed class BeAlertCapSource : IBeAlertCapSource
    {
        public const string Code = "BE-ALERT";

        private readonly HttpClient _http;
        private readonly BeAlertCapOptions _options;
        private readonly ILogger<BeAlertCapSource> _logger;

        public string SourceCode => Code;

        public BeAlertCapSource(HttpClient http, IOptions<BeAlertCapOptions> options, ILogger<BeAlertCapSource> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<EmergencyAlertBatch> FetchAsync(EmergencyAlertCursor cursor, CancellationToken cancellationToken)
        {
            var fetchedAtUtc = DateTimeOffset.UtcNow;

            if (!_options.Enabled)
            {
                _logger.LogDebug("[BE-ALERT] Source disabled.");

                return EmptyBatch(fetchedAtUtc, cursor.ETag, cursor.LastModifiedUtc);
            }

            if (!Uri.TryCreate(_options.FeedUrl, UriKind.Absolute, out var feedUri))
            {
                throw new InvalidOperationException("BE-Alert CAP FeedUrl is invalid.");
            }

            /*
             * SSRF Protection :
             * This HttpClient must not become a generic proxy.
             */
            if (!string.Equals(feedUri.Host, "publicalerts.be", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(feedUri.Host, "sandbox.publicalerts.be", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"BE-Alert host not allowed: {feedUri.Host}");
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, feedUri);

            /*
             * Conditional HTTP request.
             */
            if (!string.IsNullOrWhiteSpace(cursor.ETag))
            {
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(cursor.ETag));
            }

            if (cursor.LastModifiedUtc is not null)
            {
                request.Headers.IfModifiedSince = cursor.LastModifiedUtc;
            }

            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            /*
             * Nothing has changed since the last synchronization.
             */
            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                _logger.LogDebug("[BE-ALERT] HTTP 304 Not Modified.");

                return EmptyBatch(fetchedAtUtc, cursor.ETag, cursor.LastModifiedUtc);
            }

            response.EnsureSuccessStatusCode();

            var contentLength = response.Content.Headers.ContentLength;

            if (contentLength.HasValue && contentLength.Value > _options.MaxPayloadBytes)
            {
                throw new InvalidOperationException($"BE-Alert CAP payload too large: " + $"{contentLength.Value} bytes.");
            }

            /*
             * Provider HTTP metadata.
             */
            var etag = response.Headers.ETag?.Tag;

            var lastModifiedUtc = response.Content.Headers.LastModified;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/xml";

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            /*
             * Secure parsing of the CAP.
             */
            var capMessages = BeAlertCapParser.Parse(stream);

            /*
             * Important :
             *
             * RawEmergencyAlert reste RAW.
             *
             * The CAP-rich properties
             * (headline/severity/emergency/areas/etc.)
             * will be converted by the normalizer,
             * not here.
             */
            var alerts = capMessages
                .Where(IsUsablePublicMessage)
                .Select(cap => MapToRawEmergencyAlert(
                    cap,
                    feedUri,
                    contentType,
                    etag,
                    lastModifiedUtc,
                    fetchedAtUtc)
                )
                .ToArray();

            _logger.LogInformation("[BE-ALERT] CAP fetch completed. " + "Parsed={ParsedCount}, Accepted={AcceptedCount}, " + "ETag={ETag}.", capMessages.Count, alerts.Length, etag);

            return new EmergencyAlertBatch(
                Alerts: alerts,
                ETag: etag,
                LastModifiedUtc: lastModifiedUtc,
                ContinuationToken: null,
                FetchedAtUtc: fetchedAtUtc);
        }

        private static bool IsUsablePublicMessage(BeAlertCapMessage message)
        {
            /*
             * No private/restrictive CAP message
             * in the OutZen public engine.
             */
            if (!string.Equals(message.Scope, "Public", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            /*
             * Above all, the tests/exercises must not
             * not feed the decision engine.
             */
            if (string.Equals(message.Status, "Exercise", StringComparison.OrdinalIgnoreCase)
                || string.Equals(message.Status, "Test", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(message.Identifier))
            {
                return false;
            }

            return true;
        }

        private static RawEmergencyAlert MapToRawEmergencyAlert(BeAlertCapMessage cap, Uri sourceUri, string contentType, string? etag, DateTimeOffset? lastModifiedUtc, DateTimeOffset receivedAtUtc)
        {
            return new RawEmergencyAlert(
                SourceCode: Code,
                ExternalId: cap.Identifier,
                RawPayload: cap.RawXml,
                ContentType: contentType,
                ReceivedAtUtc: receivedAtUtc,
                SourceUri: sourceUri,
                ETag: etag,
                LastModifiedUtc: lastModifiedUtc);
        }

        private static EmergencyAlertBatch EmptyBatch(DateTimeOffset fetchedAtUtc, string? etag = null, DateTimeOffset? lastModifiedUtc = null)
        {
            return new EmergencyAlertBatch(
                Alerts: Array.Empty<RawEmergencyAlert>(),
                ETag: etag,
                LastModifiedUtc: lastModifiedUtc,
                ContinuationToken: null,
                FetchedAtUtc: fetchedAtUtc);
        }
    }
}




























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.