using Moq;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Payments;
using PaymentService.Application.Payments.Commands.ProcessRideCompleted;
using PaymentService.Application.Payments.Interfaces;
using PaymentService.Domain.Common;
using PaymentService.Domain.Payments;
using PaymentService.Application.Payments.Events;
using Xunit;

namespace PaymentService.Tests;

public class ProcessRideCompletedHandlerTests
{
    private readonly Mock<IPaymentRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPaymentProvider> _provider = new();
    private readonly Guid _trip = Guid.NewGuid();
    private readonly Guid _rider = Guid.NewGuid();
    private readonly Mock<IOutbox> _outbox = new();

    private ProcessRideCompletedHandler Handler()
        => new(_repo.Object, _uow.Object, _provider.Object, new AutoPayOptions { Token = "tok_default" }, _outbox.Object);

    private ProcessRideCompletedCommand Cmd(decimal amount = 76_000m) => new(Guid.NewGuid(), _trip, _rider, amount);

    private void ProviderReturns(ChargeResult r)
        => _provider.Setup(p => p.ChargeAsync(It.IsAny<ChargeRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(r);

    private void NeverCharged()
        => _provider.Verify(p => p.ChargeAsync(It.IsAny<ChargeRequest>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    public async Task Thanh_cong_tru_dung_so_tien_cua_event_bang_token_mac_dinh()
    {
        ProviderReturns(new ChargeResult(true, "ref", null));

        var dto = await Handler().HandleAsync(Cmd());

        Assert.Equal("Succeeded", dto.Status);
        Assert.Equal(76_000m, dto.Amount);
        Assert.Equal(2, dto.Ledger.Count);
        _provider.Verify(p => p.ChargeAsync(
            It.Is<ChargeRequest>(c => c.Amount == 76_000m && c.Token == "tok_default"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Bi_tu_choi_thi_Failed_va_khong_co_so_cai()
    {
        ProviderReturns(new ChargeResult(false, null, "The bi tu choi"));

        var dto = await Handler().HandleAsync(Cmd());

        Assert.Equal("Failed", dto.Status);
        Assert.Empty(dto.Ledger);
    }

    [Fact]
    public async Task Message_den_lan_hai_khong_tru_tien_lan_nua()
    {
        var old = Payment.Initiate(_trip, _rider, 76_000m, ProcessRideCompletedHandler.KeyFor(_trip));
        _repo.Setup(r => r.GetByKeyAsync(_rider, ProcessRideCompletedHandler.KeyFor(_trip), It.IsAny<CancellationToken>()))
             .ReturnsAsync(old);

        var dto = await Handler().HandleAsync(Cmd());

        Assert.Equal(old.Id, dto.Id);
        NeverCharged();
    }

    [Fact]
    public async Task Rider_da_tu_tra_bang_POST_payments_thi_khong_tru_lan_nua()
    {
        var manual = Payment.Initiate(_trip, _rider, 76_000m, "manual-key");
        _repo.Setup(r => r.GetActiveByTripAsync(_trip, It.IsAny<CancellationToken>())).ReturnsAsync(manual);

        var dto = await Handler().HandleAsync(Cmd());

        Assert.Equal(manual.Id, dto.Id);
        NeverCharged();
    }

    [Fact]
    public async Task So_tien_khong_hop_le_nem_DomainValidationException_va_khong_tru_tien()
    {
        await Assert.ThrowsAsync<DomainValidationException>(() => Handler().HandleAsync(Cmd(amount: 0)));
        NeverCharged();
    }

        [Fact]
    public async Task Ket_qua_duoc_ghi_vao_outbox_de_bao_cho_Trip()
    {
        ProviderReturns(new ChargeResult(true, "ref", null));

        await Handler().HandleAsync(Cmd());

        _outbox.Verify(o => o.Enqueue(PaymentRoutingKeys.Settled, It.IsAny<PaymentSettled>()), Times.Once);
    }

    [Fact]
    public async Task Message_den_lan_hai_khong_phat_lai_su_kien()
    {
        var old = Payment.Initiate(_trip, _rider, 76_000m, ProcessRideCompletedHandler.KeyFor(_trip));
        _repo.Setup(r => r.GetByKeyAsync(_rider, ProcessRideCompletedHandler.KeyFor(_trip), It.IsAny<CancellationToken>()))
             .ReturnsAsync(old);

        await Handler().HandleAsync(Cmd());

        _outbox.Verify(o => o.Enqueue(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }
}