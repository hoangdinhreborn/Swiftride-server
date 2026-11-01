using BuildingBlocks.Security;
using Moq;
using PaymentService.Application.Common.Exceptions;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Payments.Commands.CreatePayment;
using PaymentService.Application.Payments.Interfaces;
using PaymentService.Domain.Common;
using PaymentService.Domain.Payments;
using PaymentService.Application.Payments.Events;
using PaymentService.Application.Payments;
using Xunit;

namespace PaymentService.Tests;

public class CreatePaymentHandlerTests
{
    private readonly Mock<IPaymentRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<ITripGateway> _trips = new();
    private readonly Mock<IPaymentProvider> _provider = new();
    private readonly Guid _rider = Guid.NewGuid();
    private readonly Guid _tripId = Guid.NewGuid();
    private readonly Mock<IOutbox> _outbox = new();

    public CreatePaymentHandlerTests() => _user.Setup(u => u.Id).Returns(_rider);

    private PaymentSettler Settler()
        => new(_provider.Object, _uow.Object, _outbox.Object, new PaymentRetryOptions { BaseDelay = TimeSpan.Zero });

    private CreatePaymentHandler Handler() => new(_repo.Object, _uow.Object, _user.Object, _trips.Object, Settler());
    private CreatePaymentCommand Cmd(string key = "k1") => new(_tripId, "tok_ok", key);

    private void TripIs(string status = "DroppedOff", Guid? riderId = null, decimal fare = 76_000m)
        => _trips.Setup(t => t.GetTripAsync(_tripId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new TripInfo(_tripId, riderId ?? _rider, status, fare));

    private void ProviderReturns(ChargeResult result)
        => _provider.Setup(p => p.ChargeAsync(It.IsAny<ChargeRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);

    [Fact]
    public async Task Thanh_cong_lay_so_tien_tu_Trip_va_luu_hai_lan()
    {
        TripIs();
        ProviderReturns(new ChargeResult(true, "ref", null));

        var r = await Handler().HandleAsync(Cmd());

        Assert.False(r.IsReplay);
        Assert.Equal("Succeeded", r.Payment.Status);
        Assert.Equal(76_000m, r.Payment.Amount);
        Assert.Equal(2, r.Payment.Ledger.Count);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));   // Pending, rồi kết quả
    }

    [Fact]
    public async Task Bi_tu_choi_thi_Failed_va_khong_co_so_cai()
    {
        TripIs();
        ProviderReturns(new ChargeResult(false, null, "The bi tu choi"));

        var r = await Handler().HandleAsync(Cmd());

        Assert.Equal("Failed", r.Payment.Status);
        Assert.Empty(r.Payment.Ledger);
    }

    [Fact]
    public async Task Gui_lai_cung_key_tra_ket_qua_cu_va_khong_goi_cong_thanh_toan()
    {
        var old = Payment.Initiate(_tripId, _rider, 76_000m, "k1");
        _repo.Setup(r => r.GetByKeyAsync(_rider, "k1", It.IsAny<CancellationToken>())).ReturnsAsync(old);

        var r = await Handler().HandleAsync(Cmd("k1"));

        Assert.True(r.IsReplay);
        Assert.Equal(old.Id, r.Payment.Id);
        _provider.Verify(p => p.ChargeAsync(It.IsAny<ChargeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _trips.Verify(t => t.GetTripAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cung_key_khac_chuyen_nem_DomainException()
    {
        var old = Payment.Initiate(Guid.NewGuid(), _rider, 76_000m, "k1");   // chuyến khác
        _repo.Setup(r => r.GetByKeyAsync(_rider, "k1", It.IsAny<CancellationToken>())).ReturnsAsync(old);

        await Assert.ThrowsAsync<DomainException>(() => Handler().HandleAsync(Cmd("k1")));
    }

    [Fact]
    public async Task Chuyen_chua_DroppedOff_nem_DomainException_va_khong_tru_tien()
    {
        TripIs(status: "PickedUp");

        await Assert.ThrowsAsync<DomainException>(() => Handler().HandleAsync(Cmd()));

        _provider.Verify(p => p.ChargeAsync(It.IsAny<ChargeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Khong_phai_rider_cua_chuyen_nem_ForbiddenException()
    {
        TripIs(riderId: Guid.NewGuid());

        await Assert.ThrowsAsync<ForbiddenException>(() => Handler().HandleAsync(Cmd()));
    }

    [Fact]
    public async Task Chuyen_da_co_thanh_toan_nem_DomainException()
    {
        TripIs();
        _repo.Setup(r => r.GetActiveByTripAsync(_tripId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(Payment.Initiate(_tripId, _rider, 76_000m, "other"));

        await Assert.ThrowsAsync<DomainException>(() => Handler().HandleAsync(Cmd()));
    }

    [Fact]
    public async Task Thieu_key_nem_DomainValidationException()
        => await Assert.ThrowsAsync<DomainValidationException>(
            () => Handler().HandleAsync(new CreatePaymentCommand(_tripId, "tok_ok", null)));
    [Fact]
    public async Task Thanh_cong_ghi_su_kien_PaymentSettled_vao_outbox()
    {
        TripIs();
        ProviderReturns(new ChargeResult(true, "ref", null));

        await Handler().HandleAsync(Cmd());

        _outbox.Verify(o => o.Enqueue(PaymentRoutingKeys.Settled,
            It.Is<PaymentSettled>(e => e.TripId == _tripId && e.RiderId == _rider && e.Amount == 76_000m)), Times.Once);
    }

    [Fact]
    public async Task Bi_tu_choi_ghi_su_kien_PaymentFailed_vao_outbox()
    {
        TripIs();
        ProviderReturns(new ChargeResult(false, null, "The bi tu choi"));

        await Handler().HandleAsync(Cmd());

        _outbox.Verify(o => o.Enqueue(PaymentRoutingKeys.Failed, It.IsAny<PaymentFailed>()), Times.Once);
    }

    [Fact]
    public async Task Chuyen_PaymentPending_van_cho_thanh_toan_lai()
    {
        TripIs(status: "PaymentPending");
        ProviderReturns(new ChargeResult(true, "ref", null));

        var r = await Handler().HandleAsync(Cmd("k-retry"));

        Assert.Equal("Succeeded", r.Payment.Status);
    }

    [Fact]
    public async Task Thanh_toan_bang_vi_gui_dung_loai_cho_cong_thanh_toan()
    {
        TripIs();
        ProviderReturns(new ChargeResult(true, "ref", null));

        var r = await Handler().HandleAsync(new CreatePaymentCommand(_tripId, "wallet_ok", "k-wallet", "Wallet"));

        Assert.Equal("Wallet", r.Payment.Method);
        _provider.Verify(p => p.ChargeAsync(
            It.Is<ChargeRequest>(c => c.Method.Kind == PaymentMethodKind.Wallet && c.Token == "wallet_ok"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Phuong_thuc_khong_ho_tro_nem_DomainValidationException_va_khong_tru_tien()
    {
        TripIs();

        await Assert.ThrowsAsync<DomainValidationException>(
            () => Handler().HandleAsync(new CreatePaymentCommand(_tripId, "tok_ok", "k1", "Bitcoin")));

        _provider.Verify(p => p.ChargeAsync(It.IsAny<ChargeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

        private static readonly ChargeResult Approved = new(true, "ref", null);

    [Fact]
    public async Task Thanh_toan_Deferred_duoc_lam_tiep_voi_cung_Reference_khi_rider_gui_lai_voi_key_moi()
    {
        var deferred = Payment.Initiate(_tripId, _rider, 76_000m, "ride-completed:x");
        deferred.MarkDeferred("timeout");
        TripIs(status: "PaymentPending");
        _repo.Setup(r => r.GetActiveByTripAsync(_tripId, It.IsAny<CancellationToken>())).ReturnsAsync(deferred);
        ProviderReturns(Approved);

        var r = await Handler().HandleAsync(new CreatePaymentCommand(_tripId, "tok_ok", "k-new", "Card"));

        Assert.True(r.IsReplay);
        Assert.Equal(deferred.Id, r.Payment.Id);
        Assert.Equal("Succeeded", r.Payment.Status);
        _repo.Verify(x => x.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _provider.Verify(p => p.ChargeAsync(
            It.Is<ChargeRequest>(c => c.Reference == deferred.Id.ToString()), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cung_key_voi_thanh_toan_Deferred_thi_lam_tiep_va_khong_can_goi_Trip()
    {
        var deferred = Payment.Initiate(_tripId, _rider, 76_000m, "k1");
        deferred.MarkDeferred("timeout");
        _repo.Setup(r => r.GetByKeyAsync(_rider, "k1", It.IsAny<CancellationToken>())).ReturnsAsync(deferred);
        ProviderReturns(Approved);

        var r = await Handler().HandleAsync(Cmd("k1"));

        Assert.Equal("Succeeded", r.Payment.Status);
        _trips.Verify(t => t.GetTripAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Thanh_toan_Deferred_cua_nguoi_khac_khong_duoc_lam_tiep()
    {
        var other = Payment.Initiate(_tripId, Guid.NewGuid(), 76_000m, "other");
        other.MarkDeferred("timeout");
        TripIs(status: "PaymentPending");
        _repo.Setup(r => r.GetActiveByTripAsync(_tripId, It.IsAny<CancellationToken>())).ReturnsAsync(other);

        await Assert.ThrowsAsync<DomainException>(() => Handler().HandleAsync(Cmd("k-new")));

        _provider.Verify(p => p.ChargeAsync(It.IsAny<ChargeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}