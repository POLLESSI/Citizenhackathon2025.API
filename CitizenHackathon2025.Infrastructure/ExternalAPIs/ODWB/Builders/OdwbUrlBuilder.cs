using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Builders
{
    public static class OdwbUrlBuilder
    {
        public static Uri Build(string baseUrl, OdwbQuery q)
        {
            var ub = new UriBuilder(baseUrl);
            var url = ub.Uri.ToString();

            // Opendatasoft v2.1 params: where/select/group_by/order_by/limit/offset
            var dict = new Dictionary<string, string?>();

            if (!string.IsNullOrWhiteSpace(q.Where)) dict["where"] = q.Where;
            if (!string.IsNullOrWhiteSpace(q.Select)) dict["select"] = q.Select;
            if (!string.IsNullOrWhiteSpace(q.GroupBy)) dict["group_by"] = q.GroupBy;
            if (!string.IsNullOrWhiteSpace(q.OrderBy)) dict["order_by"] = q.OrderBy;
            if (q.Limit is not null) dict["limit"] = q.Limit.Value.ToString();
            if (q.Offset is not null) dict["offset"] = q.Offset.Value.ToString();

            url = QueryHelpers.AddQueryString(url, dict!);
            return new Uri(url);
        }
    }
}
