using PaymentService.Application.Common.Exceptions;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Telemetry;
using PaymentService.Application.Payments.Dtos;
using PaymentService.Application.Payments.Interfaces;
using PaymentService.Domain.Common;
using PaymentService.Domain.Payments;

namespace PaymentService.Application.Payments.Commands.CompensatePayment;

// Không phụ thuộc ICurrentUser: chạy từ message, không có HTTP request
public sealed class CompensatePaymentHandler(IPaymentRepository repo, PaymentRefunder refunder)
    : IHandler<CompensatePaymentCommand, PaymentDto>
{
    public async Task<PaymentDto> HandleAsync(CompensatePaymentCommand cmd, CancellationToken ct = default)
    {
        var payment = await repo.GetByIdAsync(cmd.PaymentId, ct)
                      ?? throw new NotFoundException($"Không tìm thấy thanh toán {cmd.PaymentId}");

        if (payment.TripId != cmd.TripId)
            throw new DomainException("Thanh toán này không thuộc chuyến được nêu trong yêu cầu bồi thường");

        switch (payment.Status)
        {
            case PaymentStatus.Refunded:
                return payment.ToDto();   // đã hoàn rồi (message trùng, hoặc Admin hoàn trước): không làm gì thêm

            case PaymentStatus.Succeeded:
                await refunder.RefundAsync(payment, $"Saga bồi thường: {cmd.Reason}", ct);
                PaymentMetrics.Record("compensated");
                return payment.ToDto();

            default:   // Pending, Deferred, Failed: chưa có tiền để hoàn, và không nên xảy ra
                throw new DomainException($"Thanh toán đang ở trạng thái {payment.Status}, không có tiền để hoàn");
        }
    }
}