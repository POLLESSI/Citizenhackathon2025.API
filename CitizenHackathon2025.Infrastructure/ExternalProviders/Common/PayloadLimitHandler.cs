namespace CitizenHackathon2025.Infrastructure.ExternalProviders.Common;

public sealed class PayloadLimitHandler : DelegatingHandler
{
    private readonly long _maxPayloadBytes;

    public PayloadLimitHandler(long maxPayloadBytes)
    {
        _maxPayloadBytes = maxPayloadBytes;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        var response = await base.SendAsync(request, ct);

        var length = response.Content.Headers.ContentLength;

        if (length.HasValue && length.Value > _maxPayloadBytes)
        {
            response.Dispose();
            throw new InvalidOperationException($"Payload too large: {length.Value} bytes.");
        }

        return response;
    }
}































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.