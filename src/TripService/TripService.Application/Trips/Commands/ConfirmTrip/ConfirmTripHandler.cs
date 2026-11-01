using BuildingBlocks.Security;
using TripService.Application.Common.Exceptions;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Dtos;
using TripService.Application.Trips.Interfaces;

namespace TripService.Application.Trips.Commands.ConfirmTrip;

public sealed class ConfirmTripHandler(ITripRepository repo, IUnitOfWork uow, ICurrentUser user, DriverDispatcher dispatcher)
    : IHandler<ConfirmTripCommand, TripDto>
{
    public async Task<TripDto> HandleAsync(ConfirmTripCommand cmd, CancellationToken ct = default)
    {
        var trip = await repo.GetRequiredAsync(cmd.TripId, ct);
        if (trip.RiderId != user.Id) throw new ForbiddenException("Bạn không phải người đặt chuyến này");

        trip.Confirm();                                  // Requested -> Searching
        await dispatcher.DispatchAsync(trip, ct);        // -> DriverOffered hoặc Failed. Matching lỗi thì ném và chưa lưu gì
        await uow.SaveChangesAsync(ct);
        return trip.ToDto();
    }
}