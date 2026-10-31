using BuildingBlocks.Security;
using PaymentService.Application.Common.Exceptions;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Payments.Dtos;
using PaymentService.Application.Payments.Interfaces;
using PaymentService.Domain.Common;
using PaymentService.Domain.Payments;
using PaymentService.Application.Payments.Events;

namespace PaymentService.Application.Payments.Commands.CreatePayment;

public sealed class CreatePaymentHandler(
    IPaymentRepository repo, IUnitOfWork uow, ICurrentUser user,
    ITripGateway trips, IPaymentProvider provider, IOutbox outbox)
    : IHandler<CreatePaymentCommand, CreatePaymentResult>
{
    public async Task<CreatePaymentResult> HandleAsync(CreatePaymentCommand cmd, CancellationToken ct = default)
    {
        if (cmd.TripId is not { } tripId || tripId == Guid.Empty)
            throw new DomainValidationException("Thiếu mã chuyến");
        if (string.IsNullOrWhiteSpace(cmd.PaymentToken))
            throw new DomainValidationException("Thiếu token thanh toán");
        var key = cmd.IdempotencyKey?.Trim();
        if (string.IsNullOrEmpty(key) || key.Length > Payment.MaxKeyLength)
            throw new DomainValidationException($"Header Idempotency-Key là bắt buộc, tối đa {Payment.MaxKeyLength} ký tự");

        // 1. Gửi lại cùng key: trả kết quả cũ, không gọi cổng thanh toán lần nữa
        var existing = await repo.GetByKeyAsync(user.Id, key, ct);
        if (existing is not null)
        {
            if (existing.TripId != tripId)
                throw new DomainException("Idempotency-Key này đã được dùng cho một chuyến khác");
            return new CreatePaymentResult(existing.ToDto(), IsReplay: true);
        }

        // 2. Số tiền lấy từ Trip Service, không tin client
        var trip = await trips.GetTripAsync(tripId, ct)
                   ?? throw new NotFoundException($"Không tìm thấy chuyến {tripId}");
        if (trip.RiderId != user.Id)
            throw new ForbiddenException("Bạn không phải người đặt chuyến này");
        if (trip.Status is not ("DroppedOff" or "PaymentPending"))
            throw new DomainException($"Chuyến đang ở trạng thái {trip.Status}, chỉ thanh toán khi DroppedOff hoặc PaymentPending");
        if (await repo.GetActiveByTripAsync(tripId, ct) is not null)
            throw new DomainException("Chuyến này đã có thanh toán");

        // 3. Lưu Pending trước khi gọi cổng thanh toán
        var payment = Payment.Initiate(tripId, user.Id, trip.QuotedFare, key);
        await repo.AddAsync(payment, ct);
        await uow.SaveChangesAsync(ct);

        // 4. Gọi cổng thanh toán, rồi cập nhật kết quả và ghi sổ cái
        var charge = await provider.ChargeAsync(
            new ChargeRequest(payment.Amount, cmd.PaymentToken.Trim(), payment.Id.ToString()), ct);

        if (charge.Approved) payment.MarkSucceeded(charge.ProviderReference ?? "");
        else payment.MarkFailed(charge.FailureReason ?? "Bị từ chối");

        outbox.EnqueueOutcome(payment);
        await uow.SaveChangesAsync(ct);
        return new CreatePaymentResult(payment.ToDto(), IsReplay: false);
    }
}