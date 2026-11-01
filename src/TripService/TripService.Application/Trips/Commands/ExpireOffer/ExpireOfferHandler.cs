using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Interfaces;
using TripService.Domain.Trips;

namespace TripService.Application.Trips.Commands.ExpireOffer;

public sealed class ExpireOfferHandler(
    ITripRepository repo, IUnitOfWork uow, DriverDispatcher dispatcher,
    OfferExpiryOptions options, TimeProvider clock)
    : IHandler<ExpireOfferCommand, bool>
{
    public async Task<bool> HandleAsync(ExpireOfferCommand cmd, CancellationToken ct = default)
    {
        var trip = await repo.GetByIdAsync(cmd.TripId, ct);

        // Đọc lại và kiểm tra lần nữa: tài xế có thể vừa trả lời trong lúc worker đang chạy
        if (trip is null || !trip.IsOfferExpired(clock.GetUtcNow().UtcDateTime, options.Ttl))
            return false;

        trip.DeclineBy(trip.OfferedDriverId!.Value);   // coi như từ chối: tính vào k, bị loại khỏi lượt sau
        if (trip.Status == TripStatus.Searching)
            await dispatcher.DispatchAsync(trip, ct);  // Matching lỗi thì ném và chưa lưu gì, lượt sau thử lại

        await uow.SaveChangesAsync(ct);
        return true;
    }
}