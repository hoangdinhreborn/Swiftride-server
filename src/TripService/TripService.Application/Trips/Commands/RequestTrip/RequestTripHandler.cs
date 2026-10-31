using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Dtos;
using TripService.Application.Trips.Interfaces;
using TripService.Domain.Trips;

namespace TripService.Application.Trips.Commands.RequestTrip;

public sealed class RequestTripHandler(ITripRepository repo, IUnitOfWork uow, ICurrentUser user)
    : IHandler<RequestTripCommand, TripDto>
{
    public async Task<TripDto> HandleAsync(RequestTripCommand cmd, CancellationToken ct = default)
    {
        var trip = Trip.Request(user.Id, cmd.Pickup, cmd.Destination);
        await repo.AddAsync(trip, ct);
        await uow.SaveChangesAsync(ct);
        return trip.ToDto();
    }
}