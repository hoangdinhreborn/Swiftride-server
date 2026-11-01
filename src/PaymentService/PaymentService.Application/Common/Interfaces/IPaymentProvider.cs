using PaymentService.Domain.Payments;

namespace PaymentService.Application.Payments.Interfaces;

public record ChargeRequest(decimal Amount, PaymentMethod Method, string Reference)
{
    public string Token => Method.Token;
}

public record ChargeResult(bool Approved, string? ProviderReference, string? FailureReason);

public interface IPaymentProvider
{
    Task<ChargeResult> ChargeAsync(ChargeRequest request, CancellationToken ct = default);
}