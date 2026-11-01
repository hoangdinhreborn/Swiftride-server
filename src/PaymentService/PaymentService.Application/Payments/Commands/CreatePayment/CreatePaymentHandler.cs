using BuildingBlocks.Security;
using PaymentService.Application.Common.Exceptions;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Payments.Dtos;
using PaymentService.Application.Payments.Interfaces;
using PaymentService.Domain.Common;
using PaymentService.Domain.Payments;

namespace PaymentService.Application.Payments.Commands.CreatePayment;

public sealed class CreatePaymentHandler(
    IPaymentRepository repo, IUnitOfWork uow, ICurrentUser user,
    ITripGateway trips, PaymentSettler settler)
    : IHandler<CreatePaymentCommand, CreatePaymentResult>
{
    public async Task<CreatePaymentResult> HandleAsync(CreatePaymentCommand cmd, CancellationToken ct = default)
    {
        if (cmd.TripId is not { } tripId || tripId == Guid.Empty)
            throw new DomainValidationException("Thiếu mã chuyến");

        // Factory chọn loại phương thức và kiểm tra token. Sai thì ném DomainValidationException (400)
        var method = PaymentMethodFactory.Create(cmd.Method, cmd.PaymentToken);

        var key = cmd.IdempotencyKey?.Trim();
        if (string.IsNullOrEmpty(key) || key.Length > Payment.MaxKeyLength)
            throw new DomainValidationException($"Header Idempotency-Key là bắt buộc, tối đa {Payment.MaxKeyLength} ký tự");

        // 1. Gửi lại cùng key: trả kết quả cũ. Riêng thanh toán đang "chờ xử lý" thì làm tiếp
        var existing = await repo.GetByKeyAsync(user.Id, key, ct);
        if (existing is not null)
        {
            if (existing.TripId != tripId)
                throw new DomainException("Idempotency-Key này đã được dùng cho một chuyến khác");
            return existing.Status == PaymentStatus.Deferred
                ? await ResumeAsync(existing, method, ct)
                : new CreatePaymentResult(existing.ToDto(), IsReplay: true);
        }

        // 2. Số tiền lấy từ Trip Service, không tin client
        var trip = await trips.GetTripAsync(tripId, ct)
                   ?? throw new NotFoundException($"Không tìm thấy chuyến {tripId}");
        if (trip.RiderId != user.Id)
            throw new ForbiddenException("Bạn không phải người đặt chuyến này");
        if (trip.Status is not ("DroppedOff" or "PaymentPending"))
            throw new DomainException($"Chuyến đang ở trạng thái {trip.Status}, chỉ thanh toán khi DroppedOff hoặc PaymentPending");

        // Chuyến đã có thanh toán "chờ xử lý" của chính rider này thì làm tiếp nó, dù dùng key mới
        var active = await repo.GetActiveByTripAsync(tripId, ct);
        if (active is not null)
        {
            if (active.Status == PaymentStatus.Deferred && active.PayerId == user.Id)
                return await ResumeAsync(active, method, ct);
            throw new DomainException("Chuyến này đã có thanh toán");
        }

        // 3. Lưu Pending trước khi gọi cổng thanh toán
        var payment = Payment.Initiate(tripId, user.Id, trip.QuotedFare, key, method.Kind);
        await repo.AddAsync(payment, ct);
        await uow.SaveChangesAsync(ct);

        // 4. Gọi cổng (có retry), lưu kết quả, sổ cái và sự kiện outbox
        await settler.SettleAsync(payment, method, ct);
        return new CreatePaymentResult(payment.ToDto(), IsReplay: false);
    }

    // IsReplay = true nghĩa là KHÔNG tạo thanh toán mới (gửi lại hoặc làm tiếp), endpoint trả 200
    private async Task<CreatePaymentResult> ResumeAsync(Payment payment, PaymentMethod method, CancellationToken ct)
    {
        payment.UseMethod(method.Kind);
        await settler.SettleAsync(payment, method, ct);   // cùng Reference = payment.Id nên không trừ tiền lặp
        return new CreatePaymentResult(payment.ToDto(), IsReplay: true);
    }
}