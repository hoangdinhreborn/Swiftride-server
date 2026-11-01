using Microsoft.EntityFrameworkCore;
using TripService.Application.Trips.Interfaces;
using TripService.Domain.Trips;

namespace TripService.Infrastructure.Persistence.Repositories;

public class TripRepository(TripDbContext db) : ITripRepository
{
    public Task<Trip?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Trips.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAsync(Trip trip, CancellationToken ct = default)
        => await db.Trips.AddAsync(trip, ct);

    public async Task<IReadOnlyList<Trip>> GetOffersForDriverAsync(Guid driverId, CancellationToken ct = default)
    => await db.Trips
        .Where(t => t.OfferedDriverId == driverId && t.Status == TripStatus.DriverOffered)
        .OrderBy(t => t.CreatedAt)
        .ToListAsync(ct);
}