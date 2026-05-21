using Microsoft.Extensions.Options;
using System.Security;

namespace CitizenHackathon2025.Infrastructure.ExternalProviders.Common;

public sealed class SsrGuardHandler : DelegatingHandler
{
    private readonly ExternalProviderOptions _options;

    public SsrGuardHandler(IOptionsSnapshot<ExternalProviderOptions> options)
    {
        _options = options.Value;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        if (request.RequestUri is null)
            throw new SecurityException("Missing RequestUri.");

        await SafeUriValidator.ValidateAsync(request.RequestUri, _options, ct);

        return await base.SendAsync(request, ct);
    }
}






























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.