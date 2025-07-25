using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.DTOs;
using CitizenHackathon2025.Domain.Services;
using CitizenHackathon2025.DTOs.Enums;
using IAggregateSuggestionService = CitizenHackathon2025.Application.Interfaces.IAggregateSuggestionService;

namespace CitizenHackathon2025.Application.Services
{
    public class AstroIAService : IAggregateSuggestionService
    {
        public Task<string> GenerateSuggestionAsync(SuggestionContextDTO context)
        {
            var moment = context.Moment switch
            {
                MomentOfDay.Dawn => "Start your day with a quiet moment in the countryside.",
                MomentOfDay.Day => "Ideal for exploring the forests of Wallonia.",
                MomentOfDay.Sunset => "Enjoy a sunset from a hill.",
                MomentOfDay.Night => "Join an observation point to contemplate the stars.",
                _ => "Discover a new OutZen atmosphere."
            };

            return Task.FromResult($"{moment} Terms : {context.Weather}, {context.Traffic}, {context.Crowd}.");
        }
    }
}























































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.