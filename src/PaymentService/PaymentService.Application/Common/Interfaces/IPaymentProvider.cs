using PaymentService.Domain.Payments;

namespace PaymentService.Application.Payments.Interfaces;

public record ChargeRequest(decimal Amount, PaymentMethod Method, string Reference)
{
    public string Token => Method.Token;
}

public record ChargeResult(bool Approved, string? ProviderReference, string? FailureReason);

// Reference = payment.Id, dùng làm idempotency key của cổng: hoàn lại nhiều lần vẫn chỉ hoàn một lần
public record RefundRequest(string ProviderReference, decimal Amount, string Reference);

public record RefundResult(bool Approved, string? FailureReason);

public interface IPaymentProvider
{
    Task<ChargeResult> ChargeAsync(ChargeRequest request, CancellationToken ct = default);
    Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken ct = default);
}