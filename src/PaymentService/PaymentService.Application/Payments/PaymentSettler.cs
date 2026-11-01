using PaymentService.Application.Common.Exceptions;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Payments.Events;
using PaymentService.Application.Payments.Interfaces;
using PaymentService.Domain.Payments;

namespace PaymentService.Application.Payments;

// Gọi cổng thanh toán cho một Payment đã được lưu (Pending hoặc Deferred), rồi lưu kết quả cuối
public sealed class PaymentSettler(
    IPaymentProvider provider, IUnitOfWork uow, IOutbox outbox, PaymentRetryOptions options)
{
    public async Task SettleAsync(Payment payment, PaymentMethod method, CancellationToken ct)
    {
        var lastError = "Cổng thanh toán không phản hồi";

        for (var attempt = 1; attempt <= options.MaxAttempts; attempt++)
        {
            payment.RecordAttempt();
            try
            {
                // Reference = payment.Id, giống nhau ở MỌI lần thử. Cổng thật dùng nó làm idempotency key,
                // nên dù lần trước đã trừ tiền rồi mới timeout thì gọi lại cũng không trừ lần hai
                var charge = await provider.ChargeAsync(
                    new ChargeRequest(payment.Amount, method, payment.Id.ToString()), ct);

                if (charge.Approved) payment.MarkSucceeded(charge.ProviderReference ?? "");
                else payment.MarkFailed(charge.FailureReason ?? "Bị từ chối");   // dứt khoát: không thử lại

                outbox.EnqueueOutcome(payment);
                await uow.SaveChangesAsync(ct);   // một transaction: kết quả + sổ cái + sự kiện
                return;
            }
            catch (PaymentProviderUnavailableException ex) when (!ct.IsCancellationRequested)
            {
                lastError = ex.Message;
                if (attempt < options.MaxAttempts) await Task.Delay(Backoff(attempt), ct);
            }
        }

        // Hết lượt: để "chờ xử lý", Trip sẽ sang PaymentPending và rider thử lại được
        payment.MarkDeferred(lastError);
        outbox.EnqueueOutcome(payment);
        await uow.SaveChangesAsync(ct);
    }

    // Exponential backoff + jitter: base, 2·base, 4·base... cộng ngẫu nhiên tới 50% base
    private TimeSpan Backoff(int attempt)
    {
        var baseMs = options.BaseDelay.TotalMilliseconds;
        return TimeSpan.FromMilliseconds(baseMs * Math.Pow(2, attempt - 1) + Random.Shared.NextDouble() * baseMs * 0.5);
    }
}