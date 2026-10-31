using TripService.Application.Common.Exceptions;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Dtos;
using TripService.Application.Trips.Interfaces;

namespace TripService.Application.Trips.Commands.DropOffTrip;

public sealed class DropOffTripHandler(ITripRepository repo, IUnitOfWork uow, ICurrentUser user)
    : IHandler<DropOffTripCommand, TripDto>
{
    public async Task<TripDto> HandleAsync(DropOffTripCommand cmd, CancellationToken ct = default)
    {
        var trip = await repo.GetRequiredAsync(cmd.TripId, ct);
        if (trip.DriverId != user.Id) throw new ForbiddenException("Bạn không phải tài xế của chuyến này");
        trip.DropOff();
        await uow.SaveChangesAsync(ct);
        return trip.ToDto();
    }
}