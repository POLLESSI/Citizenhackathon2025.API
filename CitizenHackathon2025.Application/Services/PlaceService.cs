using Microsoft.AspNetCore.SignalR.Client;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Application;
using Citizenhackathon2025.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Citizenhackathon2025.Application.Services
{
    public class PlaceService : IPlaceService
    {
#nullable disable
        private readonly IPlaceRepository _placeRepository;

        public PlaceService(IPlaceRepository placeRepository)
        {
            _placeRepository = placeRepository;
        }

        public async Task<IEnumerable<Place?>> GetLatestPlaceAsync()
        {
            var places = await _placeRepository.GetLatestPlaceAsync();
            return places;
        }

        public async Task<Place> SavePlaceAsync(Place place)
        {
            return await _placeRepository.SavePlaceAsync(place);
        }
    }
}
