using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Payments.Dtos;
using PaymentService.Application.Payments.Events;
using PaymentService.Application.Payments.Interfaces;
using PaymentService.Domain.Payments;

namespace PaymentService.Application.Payments.Commands.ProcessRideCompleted;

public sealed class ProcessRideCompletedHandler(
    IPaymentRepository repo, IUnitOfWork uow, IPaymentProvider provider, AutoPayOptions autoPay, IOutbox outbox)
    : IHandler<ProcessRideCompletedCommand, PaymentDto>
{
    public static string KeyFor(Guid tripId) => $"ride-completed:{tripId}";

    public async Task<PaymentDto> HandleAsync(ProcessRideCompletedCommand cmd, CancellationToken ct = default)
    {
        var key = KeyFor(cmd.TripId);

        var existing = await repo.GetByKeyAsync(cmd.RiderId, key, ct)
                       ?? await repo.GetActiveByTripAsync(cmd.TripId, ct);
        if (existing is not null) return existing.ToDto();

        var payment = Payment.Initiate(cmd.TripId, cmd.RiderId, cmd.Amount, key);
        await repo.AddAsync(payment, ct);
        await uow.SaveChangesAsync(ct);   // lưu Pending trước khi gọi cổng

        var charge = await provider.ChargeAsync(
            new ChargeRequest(payment.Amount, autoPay.Token, payment.Id.ToString()), ct);

        if (charge.Approved) payment.MarkSucceeded(charge.ProviderReference ?? "");
        else payment.MarkFailed(charge.FailureReason ?? "Bị từ chối");

        outbox.EnqueueOutcome(payment);
        await uow.SaveChangesAsync(ct);   // một transaction: kết quả + sổ cái + dòng outbox
        return payment.ToDto();
    }
}