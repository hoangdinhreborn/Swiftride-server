using TripService.Application.Common.Exceptions;
using TripService.Application.Common.Interfaces;
using TripService.Application.Common.Security;
using TripService.Application.Trips.Dtos;
using TripService.Application.Trips.Interfaces;

namespace TripService.Application.Trips.Queries.GetTrip;

public sealed class GetTripHandler(ITripRepository repo, ICurrentUser user)
    : IHandler<GetTripQuery, TripDto>
{
    public async Task<TripDto> HandleAsync(GetTripQuery q, CancellationToken ct = default)
    {
        var trip = await repo.GetRequiredAsync(q.TripId, ct);
        var canView = user.Role == Roles.Admin || trip.RiderId == user.Id || trip.DriverId == user.Id;
        if (!canView) throw new ForbiddenException("Bạn không có quyền xem chuyến này");
        return trip.ToDto();
    }
}