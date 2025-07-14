using Microsoft.AspNetCore.SignalR.Client;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class PlaceService : IPlaceService
    {
#nullable disable
        private readonly IPlaceRepository _placeRepository;

        public PlaceService(IPlaceRepository placeRepository)
        {
            _placeRepository = placeRepository;
        }

        public async Task<IEnumerable<Place>> GetLatestPlaceAsync()
        {
            var places = await _placeRepository.GetLatestPlaceAsync();
            return places;
        }
        public async Task<Place?> GetPlaceByIdAsync(int id)
        {
            return await _placeRepository.GetPlaceByIdAsync(id);
        }

        public async Task<Place> SavePlaceAsync(Place place)
        {
            return await _placeRepository.SavePlaceAsync(place);
        }

        public Place UpdatePlace(Place place)
        {
            try
            {
                var UpdatePlace = _placeRepository.UpdatePlace(place);
                if (UpdatePlace == null)
                {
                    throw new KeyNotFoundException("Place not found for update.");
                }
                return UpdatePlace;
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex)
            {
                Console.WriteLine($"Validation error : {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating place : {ex}");
            }
            return null; 
        }
    }
}






































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.