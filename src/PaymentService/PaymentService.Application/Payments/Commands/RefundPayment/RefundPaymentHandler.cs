using PaymentService.Application.Common.Exceptions;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Payments.Dtos;
using PaymentService.Application.Payments.Interfaces;
using PaymentService.Domain.Payments;

namespace PaymentService.Application.Payments.Commands.RefundPayment;

public sealed class RefundPaymentHandler(IPaymentRepository repo, PaymentRefunder refunder)
    : IHandler<RefundPaymentCommand, PaymentDto>
{
    public async Task<PaymentDto> HandleAsync(RefundPaymentCommand cmd, CancellationToken ct = default)
    {
        var payment = await repo.GetByIdAsync(cmd.PaymentId, ct)
                      ?? throw new NotFoundException($"Không tìm thấy thanh toán {cmd.PaymentId}");

        if (payment.Status == PaymentStatus.Refunded) return payment.ToDto();   // hoàn lần hai: không làm gì thêm

        await refunder.RefundAsync(payment, "Hoàn tiền thủ công bởi quản trị viên", ct);
        return payment.ToDto();
    }
}