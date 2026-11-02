using Moq;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Commands.ApplyPaymentResult;
using TripService.Application.Trips.Events;
using TripService.Application.Trips.Interfaces;
using TripService.Domain.Trips;
using Xunit;

namespace TripService.Tests.ApplicationTests;

public class ApplyPaymentResultTests
{
    private readonly Mock<ITripRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IOutbox> _outbox = new();
    private readonly Guid _paymentId = Guid.NewGuid();

    private ApplyPaymentResultHandler Handler() => new(_repo.Object, _uow.Object, _outbox.Object);

    private Trip Stored(Trip t)
    {
        _repo.Setup(r => r.GetByIdAsync(t.Id, It.IsAny<CancellationToken>())).ReturnsAsync(t);
        return t;
    }

    private Trip DroppedOffTrip()
    {
        var trip = TestTrips.Accepted();
        trip.PickUp();
        trip.DropOff();
        return Stored(trip);
    }

    private Trip CancelledTrip()
    {
        var trip = TestTrips.Make();
        trip.Cancel();
        return Stored(trip);
    }

    private void NoRejectionEmitted()
        => _outbox.Verify(o => o.Enqueue(It.IsAny<string>(), It.IsAny<object>()), Times.Never);

    [Fact]
    public async Task Thanh_toan_thanh_cong_chuyen_sang_Paid_va_khong_bao_thanh_cong_gia()
    {
        var trip = DroppedOffTrip();

        var r = await Handler().HandleAsync(new ApplyPaymentResultCommand(trip.Id, _paymentId, true));

        Assert.True(r.Applied);
        Assert.Equal("Paid", r.TripStatus);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        NoRejectionEmitted();
    }

    [Fact]
    public async Task Thanh_toan_that_bai_chuyen_sang_PaymentPending()
    {
        var trip = DroppedOffTrip();

        var r = await Handler().HandleAsync(new ApplyPaymentResultCommand(trip.Id, _paymentId, false));

        Assert.True(r.Applied);
        Assert.Equal("PaymentPending", r.TripStatus);
    }

    [Fact]
    public async Task Ket_qua_thanh_cong_den_hai_lan_van_Applied_va_khong_bao_thanh_toan_bi_tu_choi()
    {
        var trip = DroppedOffTrip();
        await Handler().HandleAsync(new ApplyPaymentResultCommand(trip.Id, _paymentId, true));

        var r = await Handler().HandleAsync(new ApplyPaymentResultCommand(trip.Id, _paymentId, true));

        Assert.True(r.Applied);
        NoRejectionEmitted();
    }

    // ---------- Saga bồi thường ----------

    [Fact]
    public async Task Da_tru_tien_nhung_chuyen_khong_ton_tai_thi_phat_PaymentRejected()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Trip?)null);
        var tripId = Guid.NewGuid();

        var r = await Handler().HandleAsync(new ApplyPaymentResultCommand(tripId, _paymentId, true));

        Assert.False(r.Applied);
        _outbox.Verify(o => o.Enqueue(TripRoutingKeys.PaymentRejected,
            It.Is<PaymentRejected>(e => e.PaymentId == _paymentId && e.TripId == tripId)), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Da_tru_tien_nhung_chuyen_da_huy_thi_phat_PaymentRejected_va_chuyen_khong_doi()
    {
        var trip = CancelledTrip();

        var r = await Handler().HandleAsync(new ApplyPaymentResultCommand(trip.Id, _paymentId, true));

        Assert.False(r.Applied);
        Assert.Equal("Cancelled", r.TripStatus);
        Assert.Equal(TripStatus.Cancelled, trip.Status);
        _outbox.Verify(o => o.Enqueue(TripRoutingKeys.PaymentRejected,
            It.Is<PaymentRejected>(e => e.PaymentId == _paymentId && e.TripId == trip.Id && e.Reason.Length > 0)), Times.Once);
    }

    [Fact]
    public async Task Thanh_toan_that_bai_ma_chuyen_da_huy_thi_khong_can_boi_thuong()
    {
        var trip = CancelledTrip();

        var r = await Handler().HandleAsync(new ApplyPaymentResultCommand(trip.Id, _paymentId, false));

        Assert.False(r.Applied);
        NoRejectionEmitted();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}