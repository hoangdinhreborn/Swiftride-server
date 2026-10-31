using TripService.Application.Common.Exceptions;
using TripService.Domain.Trips;

namespace TripService.Application.Trips.Interfaces;

public static class TripRepositoryExtensions
{
    public static async Task<Trip> GetRequiredAsync(this ITripRepository repo, Guid id, CancellationToken ct = default)
        => await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Không tìm thấy chuyến {id}");
}