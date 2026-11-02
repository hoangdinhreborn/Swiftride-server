using PaymentService.Domain.Payments;

namespace PaymentService.Application.Payments.Dtos;

public static class PaymentMappings
{
        public static PaymentDto ToDto(this Payment p)
     => new(p.Id, p.TripId, p.PayerId, p.Amount, p.Status.ToString(), p.Method.ToString(), p.Attempts,
            p.FailureReason, p.RefundReason, p.CreatedAt,
            p.Entries.Select(e => new LedgerEntryDto(e.Account.ToString(), e.Direction.ToString(), e.Amount)).ToList());
}