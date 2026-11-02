using Moq;
using PaymentService.Application.Common.Exceptions;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Payments;
using PaymentService.Application.Payments.Commands.CompensatePayment;
using PaymentService.Application.Payments.Interfaces;
using PaymentService.Domain.Common;
using PaymentService.Domain.Payments;
using Xunit;

namespace PaymentService.Tests;

public class CompensationTests
{
    private readonly Mock<IPaymentRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPaymentProvider> _provider = new();
    private readonly Guid _trip = Guid.NewGuid();

    private CompensatePaymentHandler Handler()
        => new(_repo.Object, new PaymentRefunder(_provider.Object, _uow.Object));

    private Payment Stored(Action<Payment>? prepare = null)
    {
        var p = Payment.Initiate(_trip, Guid.NewGuid(), 76_000m, "k");
        prepare?.Invoke(p);
        _repo.Setup(r => r.GetByIdAsync(p.Id, It.IsAny<CancellationToken>())).ReturnsAsync(p);
        return p;
    }

    private void ProviderRefunds(bool approved = true)
        => _provider.Setup(p => p.RefundAsync(It.IsAny<RefundRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new RefundResult(approved, approved ? null : "tu choi"));

    private void ProviderNeverCalled()
        => _provider.Verify(p => p.RefundAsync(It.IsAny<RefundRequest>(), It.IsAny<CancellationToken>()), Times.Never);

    private static void Charged(Payment p) => p.MarkSucceeded("ref");

    [Fact]
    public async Task Thanh_toan_Succeeded_duoc_hoan_tien_dao_so_cai_va_ghi_ly_do()
    {
        var p = Stored(Charged);
        ProviderRefunds();

        var dto = await Handler().HandleAsync(new CompensatePaymentCommand(p.Id, _trip, "Chuyen da huy"));

        Assert.Equal("Refunded", dto.Status);
        Assert.Equal(4, dto.Ledger.Count);
        Assert.Contains("Chuyen da huy", dto.RefundReason);
        _provider.Verify(x => x.RefundAsync(
            It.Is<RefundRequest>(r => r.Reference == p.Id.ToString() && r.Amount == 76_000m && r.ProviderReference == "ref"),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Da_hoan_roi_thi_khong_goi_cong_va_khong_luu()
    {
        var p = Stored(x => { Charged(x); x.Refund("truoc do"); });

        var dto = await Handler().HandleAsync(new CompensatePaymentCommand(p.Id, _trip, "lan hai"));

        Assert.Equal("Refunded", dto.Status);
        Assert.Equal(4, dto.Ledger.Count);
        ProviderNeverCalled();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Khong_tim_thay_thanh_toan_nem_NotFoundException()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Payment?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => Handler().HandleAsync(new CompensatePaymentCommand(Guid.NewGuid(), _trip, "x")));
    }

    [Fact]
    public async Task Sai_chuyen_thi_khong_hoan_tien()
    {
        var p = Stored(Charged);

        await Assert.ThrowsAsync<DomainException>(
            () => Handler().HandleAsync(new CompensatePaymentCommand(p.Id, Guid.NewGuid(), "x")));

        ProviderNeverCalled();
    }

    [Fact]
    public async Task Thanh_toan_chua_Succeeded_thi_khong_co_tien_de_hoan()
    {
        var deferred = Stored(x => x.MarkDeferred("timeout"));

        await Assert.ThrowsAsync<DomainException>(
            () => Handler().HandleAsync(new CompensatePaymentCommand(deferred.Id, _trip, "x")));

        ProviderNeverCalled();
    }

    [Fact]
    public async Task Cong_tu_choi_hoan_tien_thi_giu_nguyen_Succeeded_va_khong_dao_so_cai()
    {
        var p = Stored(Charged);
        ProviderRefunds(approved: false);

        await Assert.ThrowsAsync<DomainException>(
            () => Handler().HandleAsync(new CompensatePaymentCommand(p.Id, _trip, "x")));

        Assert.Equal(PaymentStatus.Succeeded, p.Status);
        Assert.Equal(2, p.Entries.Count);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cong_khong_phan_hoi_thi_nem_loi_tam_thoi_de_consumer_thu_lai()
    {
        var p = Stored(Charged);
        _provider.Setup(x => x.RefundAsync(It.IsAny<RefundRequest>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new PaymentProviderUnavailableException("timeout"));

        await Assert.ThrowsAsync<PaymentProviderUnavailableException>(
            () => Handler().HandleAsync(new CompensatePaymentCommand(p.Id, _trip, "x")));

        Assert.Equal(PaymentStatus.Succeeded, p.Status);
    }
}