namespace PaymentService.Application.Payments.Interfaces;

public record ChargeRequest(decimal Amount, string Token, string Reference);

public record ChargeResult(bool Approved, string? ProviderReference, string? FailureReason);

public interface IPaymentProvider
{
    Task<ChargeResult> ChargeAsync(ChargeRequest request, CancellationToken ct = default);
}