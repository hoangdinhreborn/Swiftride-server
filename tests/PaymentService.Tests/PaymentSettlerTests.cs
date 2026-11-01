using Moq;
using PaymentService.Application.Common.Exceptions;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Payments;
using PaymentService.Application.Payments.Events;
using PaymentService.Application.Payments.Interfaces;
using PaymentService.Domain.Payments;
using Xunit;

namespace PaymentService.Tests;

public class PaymentSettlerTests
{
    private readonly Mock<IPaymentProvider> _provider = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IOutbox> _outbox = new();

    private PaymentSettler Settler(int maxAttempts = 3)
        => new(_provider.Object, _uow.Object, _outbox.Object,
               new PaymentRetryOptions { MaxAttempts = maxAttempts, BaseDelay = TimeSpan.Zero });

    private static Payment NewPayment() => Payment.Initiate(Guid.NewGuid(), Guid.NewGuid(), 76_000m, "k");
    private static PaymentMethod Card() => PaymentMethodFactory.Create("Card", "tok_ok");
    private static PaymentProviderUnavailableException Timeout() => new("timeout");

    private int Calls(Guid paymentId)
    {
        var n = 0;
        foreach (var i in _provider.Invocations)
            if (i.Arguments[0] is ChargeRequest c && c.Reference == paymentId.ToString()) n++;
        return n;
    }

    [Fact]
    public async Task Loi_tam_thoi_roi_thanh_cong_thi_thu_lai_voi_cung_Reference()
    {
        _provider.SetupSequence(p => p.ChargeAsync(It.IsAny<ChargeRequest>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(Timeout())
                 .ThrowsAsync(Timeout())
                 .ReturnsAsync(new ChargeResult(true, "ref", null));
        var payment = NewPayment();

        await Settler().SettleAsync(payment, Card(), default);

        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(3, payment.Attempts);
        Assert.Equal(3, Calls(payment.Id));   // cả 3 lần cùng Reference = payment.Id
        _outbox.Verify(o => o.Enqueue(PaymentRoutingKeys.Settled, It.IsAny<PaymentSettled>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Het_luot_thu_thi_Deferred_khong_ghi_so_cai_va_phat_PaymentFailed()
    {
        _provider.Setup(p => p.ChargeAsync(It.IsAny<ChargeRequest>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(Timeout());
        var payment = NewPayment();

        await Settler().SettleAsync(payment, Card(), default);

        Assert.Equal(PaymentStatus.Deferred, payment.Status);
        Assert.Equal(3, payment.Attempts);
        Assert.Empty(payment.Entries);
        Assert.Equal("timeout", payment.FailureReason);
        _outbox.Verify(o => o.Enqueue(PaymentRoutingKeys.Failed,
            It.Is<PaymentFailed>(e => e.PaymentId == payment.Id && e.Reason == "timeout")), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Bi_tu_choi_la_ket_qua_dut_khoat_khong_thu_lai()
    {
        _provider.Setup(p => p.ChargeAsync(It.IsAny<ChargeRequest>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new ChargeResult(false, null, "The bi tu choi"));
        var payment = NewPayment();

        await Settler().SettleAsync(payment, Card(), default);

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal(1, payment.Attempts);
        Assert.Equal(1, Calls(payment.Id));
    }

    [Fact]
    public async Task Lam_tiep_thanh_toan_Deferred_cong_don_so_lan_thu()
    {
        var payment = NewPayment();
        payment.RecordAttempt(); payment.RecordAttempt(); payment.RecordAttempt();
        payment.MarkDeferred("timeout");
        _provider.Setup(p => p.ChargeAsync(It.IsAny<ChargeRequest>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new ChargeResult(true, "ref", null));

        await Settler().SettleAsync(payment, Card(), default);

        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(4, payment.Attempts);
    }
}