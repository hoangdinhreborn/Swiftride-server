using System.Collections.Concurrent;
using PaymentService.Application.Common.Exceptions;
using PaymentService.Application.Payments.Interfaces;
using PaymentService.Domain.Payments;

namespace PaymentService.Infrastructure.Providers;

// Giả lập cổng thanh toán. Thực tế: mỗi loại phương thức gọi một cổng riêng, và Reference được
// truyền làm idempotency key của cổng (ví dụ header Idempotency-Key của Stripe)
public class FakePaymentProvider : IPaymentProvider
{
    private readonly ConcurrentDictionary<string, ChargeResult> _settled = new();   // idempotency theo Reference
    private readonly ConcurrentDictionary<string, int> _calls = new();

    public Task<ChargeResult> ChargeAsync(ChargeRequest r, CancellationToken ct = default)
    {
        // Cùng Reference thì trả đúng kết quả cũ, không "trừ tiền" lần hai
        if (_settled.TryGetValue(r.Reference, out var previous)) return Task.FromResult(previous);

        var call = _calls.AddOrUpdate(r.Reference, 1, (_, n) => n + 1);
        if (r.Token == "tok_timeout" || (r.Token == "tok_flaky" && call <= 2))
            throw new PaymentProviderUnavailableException("Cổng thanh toán không phản hồi (giả lập)");

        var result = r.Method.Kind switch
        {
            PaymentMethodKind.Card   => r.Token == "tok_declined" ? Declined("Thẻ bị từ chối") : Approved(),
            PaymentMethodKind.Wallet => r.Token == "wallet_empty" ? Declined("Số dư ví không đủ") : Approved(),
            _ => Declined("Phương thức thanh toán chưa được hỗ trợ")
        };
        return Task.FromResult(_settled.GetOrAdd(r.Reference, result));
    }

    private static ChargeResult Approved() => new(true, $"fake_{Guid.NewGuid():N}", null);
    private static ChargeResult Declined(string reason) => new(false, null, reason);
}