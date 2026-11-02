using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Payments.Interfaces;
using PaymentService.Domain.Common;
using PaymentService.Domain.Payments;

namespace PaymentService.Application.Payments;

public sealed class PaymentRefunder(IPaymentProvider provider, IUnitOfWork uow)
{
    public async Task RefundAsync(Payment payment, string reason, CancellationToken ct)
    {
        // Kiểm tra TRƯỚC khi gọi cổng, để không bao giờ hoàn tiền thật cho thanh toán không đủ điều kiện
        if (payment.Status != PaymentStatus.Succeeded)
            throw new DomainException($"Thanh toán đang ở trạng thái {payment.Status}, chỉ hoàn tiền khi Succeeded");

        // Gọi cổng trước, lưu sau. Lưu hỏng thì message giao lại, gọi cổng lần nữa với cùng Reference và không hoàn hai lần
        var result = await provider.RefundAsync(
            new RefundRequest(payment.ProviderReference ?? "", payment.Amount, payment.Id.ToString()), ct);
        if (!result.Approved)
            throw new DomainException($"Cổng thanh toán từ chối hoàn tiền: {result.FailureReason}");

        payment.Refund(reason);
        await uow.SaveChangesAsync(ct);
    }
}