using CitizenHackathon2025.Domain.Services;
using CitizenHackathon2025.Shared.Enums;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class AstroIAService : IAggregateSuggestionService
    {
        public Task<string> GenerateSuggestionAsync(SuggestionContextDTO context)
        {
            var moment = context.Moment switch
            {
                MomentOfDay.Dawn => "Commencez votre journée par un moment calme à la campagne.",
                MomentOfDay.Day => "Idéal pour explorer les forêts de la Wallonie.",
                MomentOfDay.Sunset => "Profitez d’un coucher de soleil depuis une colline.",
                MomentOfDay.Night => "Rejoignez un point d’observation pour contempler les étoiles.",
                _ => "Découvrez une nouvelle ambiance OutZen."
            };

            return Task.FromResult($"{moment} Conditions : {context.Weather}, {context.Traffic}, {context.Crowd}.");
        }
    }
}
