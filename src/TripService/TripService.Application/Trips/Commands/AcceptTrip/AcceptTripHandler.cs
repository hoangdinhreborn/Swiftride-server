using BuildingBlocks.Security;
using TripService.Application.Common.Exceptions;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Dtos;
using TripService.Application.Trips.Events;
using TripService.Application.Trips.Interfaces;

namespace TripService.Application.Trips.Commands.AcceptTrip;

public sealed class AcceptTripHandler(ITripRepository repo, IUnitOfWork uow, ICurrentUser user, IOutbox outbox)
    : IHandler<AcceptTripCommand, TripDto>
{
    public async Task<TripDto> HandleAsync(AcceptTripCommand cmd, CancellationToken ct = default)
    {
        var trip = await repo.GetRequiredAsync(cmd.TripId, ct);
        if (trip.OfferedDriverId != user.Id) throw new ForbiddenException("Chuyến này không được đề xuất cho bạn");

        trip.AcceptBy(user.Id);
        outbox.Enqueue(TripRoutingKeys.Accepted,
            new TripAccepted(Guid.NewGuid(), trip.Id, trip.RiderId, user.Id, DateTime.UtcNow));

        await uow.SaveChangesAsync(ct);   // một transaction: trạng thái + sự kiện
        return trip.ToDto();
    }
}