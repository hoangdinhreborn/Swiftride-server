using TripService.Domain.Trips;

namespace TripService.Application.Trips.Interfaces;

public interface ITripRepository
{
    Task<Trip?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Trip trip, CancellationToken ct = default);
    Task<IReadOnlyList<Trip>> GetOffersForDriverAsync(Guid driverId, CancellationToken ct = default);
}