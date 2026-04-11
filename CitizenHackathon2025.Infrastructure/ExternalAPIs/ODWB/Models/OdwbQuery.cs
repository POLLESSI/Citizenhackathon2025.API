using Microsoft.AspNetCore.WebUtilities;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Models
{
    public sealed record OdwbQuery(
    string? Where = null,
    string? Select = null,
    string? GroupBy = null,
    string? OrderBy = null,
    int? Limit = null,
    int? Offset = null);
}















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.