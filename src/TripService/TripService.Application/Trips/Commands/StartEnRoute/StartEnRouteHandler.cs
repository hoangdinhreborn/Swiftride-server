using BuildingBlocks.Security;
using TripService.Application.Common.Exceptions;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Dtos;
using TripService.Application.Trips.Interfaces;

namespace TripService.Application.Trips.Commands.StartEnRoute;

public sealed class StartEnRouteHandler(ITripRepository repo, IUnitOfWork uow, ICurrentUser user)
    : IHandler<StartEnRouteCommand, TripDto>
{
    public async Task<TripDto> HandleAsync(StartEnRouteCommand cmd, CancellationToken ct = default)
    {
        var trip = await repo.GetRequiredAsync(cmd.TripId, ct);
        if (trip.DriverId != user.Id) throw new ForbiddenException("Bạn không phải tài xế của chuyến này");

        trip.StartEnRoute();
        await uow.SaveChangesAsync(ct);
        return trip.ToDto();
    }
}