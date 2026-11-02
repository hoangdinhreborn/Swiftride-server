namespace PaymentService.Application.Payments.Commands.CompensatePayment;

public record CompensatePaymentCommand(Guid PaymentId, Guid TripId, string Reason);