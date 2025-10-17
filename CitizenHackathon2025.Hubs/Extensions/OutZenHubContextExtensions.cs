// CitizenHackathon2025.Hubs/Extensions/OutZenHubContextExtensions.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Hubs.Extensions
{
    public static class OutZenHubContextExtensions
    {
        public static Task SendNewSuggestionToEvent(this IHubContext<OutZenHub, IOutZenClient> ctx, string eventId, Suggestion dto)
            => ctx.Clients.Group(OutZenHubMethods.Groups.BuildEventGroup(eventId)).NewSuggestion(dto);

        public static Task SendCrowdInfoUpdatedToEvent(this IHubContext<OutZenHub, IOutZenClient> ctx, string eventId, object dto)
            => ctx.Clients.Group(OutZenHubMethods.Groups.BuildEventGroup(eventId)).CrowdInfoUpdated(dto);

        public static Task SendSuggestionsUpdatedToEvent(this IHubContext<OutZenHub, IOutZenClient> ctx, string eventId, IEnumerable<object> suggestions)
            => ctx.Clients.Group(OutZenHubMethods.Groups.BuildEventGroup(eventId)).SuggestionsUpdated(suggestions);

        public static Task SendWeatherUpdatedToEvent(this IHubContext<OutZenHub, IOutZenClient> ctx, string eventId, object forecast)
            => ctx.Clients.Group(OutZenHubMethods.Groups.BuildEventGroup(eventId)).WeatherUpdated(forecast);

        public static Task SendTrafficUpdatedToEvent(this IHubContext<OutZenHub, IOutZenClient> ctx, string eventId, object traffic)
            => ctx.Clients.Group(OutZenHubMethods.Groups.BuildEventGroup(eventId)).TrafficUpdated(traffic);
    }
}














































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.