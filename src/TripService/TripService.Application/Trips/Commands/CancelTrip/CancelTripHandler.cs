using BuildingBlocks.Security;
using TripService.Application.Common.Exceptions;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Dtos;
using TripService.Application.Trips.Interfaces;

namespace TripService.Application.Trips.Commands.CancelTrip;

public sealed class CancelTripHandler(ITripRepository repo, IUnitOfWork uow, ICurrentUser user)
    : IHandler<CancelTripCommand, TripDto>
{
    public async Task<TripDto> HandleAsync(CancelTripCommand cmd, CancellationToken ct = default)
    {
        var trip = await repo.GetRequiredAsync(cmd.TripId, ct);
        if (trip.RiderId != user.Id) throw new ForbiddenException("Bạn không phải người đặt chuyến này");

        trip.Cancel();   // từ chối giá (Requested), hoặc hủy khi chưa đón khách
        await uow.SaveChangesAsync(ct);
        return trip.ToDto();
    }
}