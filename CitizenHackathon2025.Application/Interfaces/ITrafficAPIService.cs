using Citizenhackathon2025.Shared.DTOs;

namespace Citizenhackathon2025.Application.Interfaces
{
    public interface ITrafficApiService
    {
        /// <summary>
        /// Retrieves traffic conditions from the Waze API (Connected Citizens).
        /// </summary>
        Task<TrafficConditionDTO?> GetCurrentTrafficAsync(double latitude, double longitude);
    }
}
