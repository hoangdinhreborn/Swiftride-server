using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Payments.Dtos;
using PaymentService.Application.Payments.Interfaces;
using PaymentService.Domain.Payments;

namespace PaymentService.Application.Payments.Commands.ProcessRideCompleted;

// Không phụ thuộc ICurrentUser: handler này chạy từ message, không có HTTP request
public sealed class ProcessRideCompletedHandler(
    IPaymentRepository repo, IUnitOfWork uow, AutoPayOptions autoPay, PaymentSettler settler)
    : IHandler<ProcessRideCompletedCommand, PaymentDto>
{
    public static string KeyFor(Guid tripId) => $"ride-completed:{tripId}";

    public async Task<PaymentDto> HandleAsync(ProcessRideCompletedCommand cmd, CancellationToken ct = default)
    {
        var key = KeyFor(cmd.TripId);

        var existing = await repo.GetByKeyAsync(cmd.RiderId, key, ct)
                       ?? await repo.GetActiveByTripAsync(cmd.TripId, ct);

        // Đã có kết quả (thành công, thất bại, chờ xử lý, đã hoàn): trả lại, không phát lại sự kiện.
        // Thanh toán "chờ xử lý" do rider thử lại, không phải việc của message trùng
        if (existing is not null && existing.Status != PaymentStatus.Pending)
            return existing.ToDto();

        // Cấu hình AutoPay sai thì ném DomainValidationException, message vào DLQ ngay và dễ thấy
        var method = PaymentMethodFactory.Create(autoPay.Method, autoPay.Token);

        Payment payment;
        if (existing is null)
        {
            payment = Payment.Initiate(cmd.TripId, cmd.RiderId, cmd.Amount, key, method.Kind);   // số tiền sai ném DomainValidationException
            await repo.AddAsync(payment, ct);
            await uow.SaveChangesAsync(ct);   // lưu Pending trước khi gọi cổng
        }
        else
        {
            // Pending mà message được giao lại: lần xử lý trước dừng giữa chừng (ví dụ service chết
            // giữa lúc lưu Pending và gọi cổng). Làm tiếp với cùng Reference
            payment = existing;
            payment.UseMethod(method.Kind);
        }

        await settler.SettleAsync(payment, method, ct);
        return payment.ToDto();
    }
}