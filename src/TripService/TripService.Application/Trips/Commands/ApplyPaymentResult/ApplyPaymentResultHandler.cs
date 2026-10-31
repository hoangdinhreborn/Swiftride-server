using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Dtos;
using TripService.Application.Trips.Interfaces;

namespace TripService.Application.Trips.Commands.ApplyPaymentResult;

public sealed class ApplyPaymentResultHandler(ITripRepository repo, IUnitOfWork uow)
    : IHandler<ApplyPaymentResultCommand, TripDto>
{
    public async Task<TripDto> HandleAsync(ApplyPaymentResultCommand cmd, CancellationToken ct = default)
    {
        var trip = await repo.GetRequiredAsync(cmd.TripId, ct);

        if (cmd.Succeeded) trip.MarkPaid();
        else trip.MarkPaymentPending();

        await uow.SaveChangesAsync(ct);
        return trip.ToDto();
    }
}