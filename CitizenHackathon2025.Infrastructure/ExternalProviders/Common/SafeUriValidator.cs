using System.Net;
using System.Net.Sockets;
using System.Security;

namespace CitizenHackathon2025.Infrastructure.ExternalProviders.Common;

public static class SafeUriValidator
{
    public static async Task ValidateAsync(
        Uri uri,
        ExternalProviderOptions options,
        CancellationToken ct = default)
    {
        if (!uri.IsAbsoluteUri)
            throw new SecurityException("External URI must be absolute.");

        if (options.RequireHttps && uri.Scheme != Uri.UriSchemeHttps)
            throw new SecurityException("Only HTTPS is allowed for this provider.");

        if (!options.AllowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
            throw new SecurityException($"Host '{uri.Host}' is not allowed.");

        var addresses = await Dns.GetHostAddressesAsync(uri.Host, ct);

        foreach (var ip in addresses)
        {
            if (IsDangerous(ip, options.AllowLoopback))
                throw new SecurityException($"Blocked dangerous resolved IP: {ip}");
        }
    }

    private static bool IsDangerous(IPAddress ip, bool allowLoopback)
    {
        if (IPAddress.IsLoopback(ip))
            return !allowLoopback;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();

            return
                b[0] == 0 ||
                b[0] == 10 ||
                b[0] == 127 ||
                b[0] == 169 && b[1] == 254 ||
                b[0] == 172 && b[1] >= 16 && b[1] <= 31 ||
                b[0] == 192 && b[1] == 168;
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return ip.IsIPv6LinkLocal ||
                   ip.IsIPv6SiteLocal ||
                   ip.Equals(IPAddress.IPv6Loopback);
        }

        return true;
    }
}




















































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.