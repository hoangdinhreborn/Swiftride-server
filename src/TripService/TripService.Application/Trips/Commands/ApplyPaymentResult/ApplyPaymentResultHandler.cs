using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Events;
using TripService.Application.Trips.Interfaces;
using TripService.Domain.Common;

namespace TripService.Application.Trips.Commands.ApplyPaymentResult;

public sealed class ApplyPaymentResultHandler(ITripRepository repo, IUnitOfWork uow, IOutbox outbox)
    : IHandler<ApplyPaymentResultCommand, ApplyPaymentResultOutcome>
{
    public async Task<ApplyPaymentResultOutcome> HandleAsync(ApplyPaymentResultCommand cmd, CancellationToken ct = default)
    {
        var trip = await repo.GetByIdAsync(cmd.TripId, ct);
        if (trip is null)
            return await RejectAsync(cmd, "NotFound", $"Không tìm thấy chuyến {cmd.TripId}", ct);

        try
        {
            if (cmd.Succeeded) trip.MarkPaid();
            else trip.MarkPaymentPending();
        }
        catch (DomainException ex)   // State pattern từ chối, và Trip không bị đổi gì
        {
            return await RejectAsync(cmd, trip.Status.ToString(), ex.Message, ct);
        }

        await uow.SaveChangesAsync(ct);
        return new ApplyPaymentResultOutcome(true, trip.Status.ToString());
    }

    private async Task<ApplyPaymentResultOutcome> RejectAsync(
        ApplyPaymentResultCommand cmd, string tripStatus, string reason, CancellationToken ct)
    {
        // Chỉ khi tiền đã bị trừ (payment.settled) mới cần bồi thường. payment.failed thì không có gì để hoàn
        if (cmd.Succeeded)
        {
            outbox.Enqueue(TripRoutingKeys.PaymentRejected,
                new PaymentRejected(Guid.NewGuid(), cmd.PaymentId, cmd.TripId, reason, DateTime.UtcNow));
            await uow.SaveChangesAsync(ct);   // chỉ lưu dòng outbox, chuyến không đổi
        }
        return new ApplyPaymentResultOutcome(false, tripStatus, reason);
    }
}