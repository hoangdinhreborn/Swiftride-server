using BuildingBlocks.Security;
using TripService.Application.Common.Exceptions;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Dtos;
using TripService.Application.Trips.Interfaces;
using TripService.Domain.Trips;

namespace TripService.Application.Trips.Commands.DeclineTrip;

public sealed class DeclineTripHandler(ITripRepository repo, IUnitOfWork uow, ICurrentUser user, DriverDispatcher dispatcher)
    : IHandler<DeclineTripCommand, TripDto>
{
    public async Task<TripDto> HandleAsync(DeclineTripCommand cmd, CancellationToken ct = default)
    {
        var trip = await repo.GetRequiredAsync(cmd.TripId, ct);
        if (trip.OfferedDriverId != user.Id) throw new ForbiddenException("Chuyến này không được đề xuất cho bạn");

        trip.DeclineBy(user.Id);   // -> Searching (<= k lần) hoặc Failed (> k lần)
        if (trip.Status == TripStatus.Searching)
            await dispatcher.DispatchAsync(trip, ct);

        await uow.SaveChangesAsync(ct);
        return trip.ToDto();
    }
}