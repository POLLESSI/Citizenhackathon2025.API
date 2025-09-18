using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class PlaceService : IPlaceService
    {
#nullable disable
        private readonly IPlaceRepository _repo;

        public PlaceService(IPlaceRepository repo)
        {
            _repo = repo;
        }

        public Task<Place> GetByIdAsync(int id, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Place>> GetLatestPlaceAsync(int limit = 200, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var items = await _repo.GetLatestPlaceAsync();

            // items: IEnumerable<Place?> -> we filter the nulls, we "unannotate" with            !
            return (items ?? Enumerable.Empty<Place?>())
                   .Where(p => p is not null)
                   .Select(p => p!)   // p! : Place (non-nullable)
                   .ToList();
        }

        public async Task<Place?> GetPlaceByIdAsync(int id)
        {
            return await _repo.GetPlaceByIdAsync(id);
        }

        public Task<Place> SaveAsync(Place place, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Place> SavePlaceAsync(Place place)
        {
            return await _repo.SavePlaceAsync(place);
        }

        public Place UpdatePlace(Place place)
        {
            try
            {
                var UpdatePlace = _repo.UpdatePlace(place);
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