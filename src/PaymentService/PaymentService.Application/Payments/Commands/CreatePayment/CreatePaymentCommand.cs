using PaymentService.Application.Payments.Dtos;

namespace PaymentService.Application.Payments.Commands.CreatePayment;

public record CreatePaymentCommand(Guid? TripId, string? PaymentToken, string? IdempotencyKey, string? Method = null);
public record CreatePaymentResult(PaymentDto Payment, bool IsReplay);