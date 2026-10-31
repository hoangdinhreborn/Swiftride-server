using PaymentService.Application.Payments.Interfaces;

namespace PaymentService.Infrastructure.Providers;

// Giả lập cổng thanh toán. Thực tế: gọi Stripe/VNPay/MoMo và truyền Reference làm idempotency key của họ
public class FakePaymentProvider : IPaymentProvider
{
    public Task<ChargeResult> ChargeAsync(ChargeRequest r, CancellationToken ct = default)
        => Task.FromResult(r.Token == "tok_declined"
            ? new ChargeResult(false, null, "Thẻ bị từ chối")
            : new ChargeResult(true, $"fake_{Guid.NewGuid():N}", null));
}