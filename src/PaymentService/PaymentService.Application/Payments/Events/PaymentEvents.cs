using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Payments;
using PaymentService.Application.Common.Telemetry;

namespace PaymentService.Application.Payments.Events;

public static class PaymentRoutingKeys
{
    public const string Settled = "payment.settled";
    public const string Failed = "payment.failed";
}

public record PaymentSettled(Guid EventId, Guid PaymentId, Guid TripId, Guid RiderId, decimal Amount, DateTime OccurredAt);

public record PaymentFailed(Guid EventId, Guid PaymentId, Guid TripId, Guid RiderId, decimal Amount, string Reason, DateTime OccurredAt);

public static class OutboxExtensions
{
    // Ghi sự kiện tương ứng với kết quả của thanh toán. Pending hoặc Refunded thì không phát gì
    public static void EnqueueOutcome(this IOutbox outbox, Payment p)
    {
        switch (p.Status)
        {
            case PaymentStatus.Succeeded:
                PaymentMetrics.Record("succeeded");
                outbox.Enqueue(PaymentRoutingKeys.Settled,
                    new PaymentSettled(Guid.NewGuid(), p.Id, p.TripId, p.PayerId, p.Amount, DateTime.UtcNow));
                break;
            case PaymentStatus.Failed:
                PaymentMetrics.Record("failed");
                outbox.Enqueue(PaymentRoutingKeys.Failed,
                    new PaymentFailed(Guid.NewGuid(), p.Id, p.TripId, p.PayerId, p.Amount,
                        p.FailureReason ?? "Bị từ chối", DateTime.UtcNow));
                break;
        }
    }
}