using BuildingBlocks.Security;
using TripService.Application.Common.Exceptions;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Dtos;
using TripService.Application.Trips.Interfaces;

namespace TripService.Application.Trips.Commands.AcceptTrip;

public sealed class AcceptTripHandler(ITripRepository repo, IUnitOfWork uow, ICurrentUser user)
    : IHandler<AcceptTripCommand, TripDto>
{
    public async Task<TripDto> HandleAsync(AcceptTripCommand cmd, CancellationToken ct = default)
    {
        var trip = await repo.GetRequiredAsync(cmd.TripId, ct);
        if (trip.OfferedDriverId != user.Id) throw new ForbiddenException("Chuyến này không được đề xuất cho bạn");

        trip.AcceptBy(user.Id);
        await uow.SaveChangesAsync(ct);
        return trip.ToDto();
    }
}