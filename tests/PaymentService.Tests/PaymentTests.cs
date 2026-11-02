using PaymentService.Domain.Common;
using PaymentService.Domain.Payments;
using Xunit;

namespace PaymentService.Tests;

public class PaymentTests
{
    private static Payment New(decimal amount = 76_000m) => Payment.Initiate(Guid.NewGuid(), Guid.NewGuid(), amount, "key-1");

    private static decimal Net(Payment p)
        => p.Entries.Sum(e => e.Direction == EntryDirection.Debit ? e.Amount : -e.Amount);

    [Fact]
    public void Initiate_tao_Pending_va_chua_co_but_toan()
    {
        var p = New();
        Assert.Equal(PaymentStatus.Pending, p.Status);
        Assert.Empty(p.Entries);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Initiate_so_tien_khong_duong_nem_loi(decimal amount)
        => Assert.Throws<DomainValidationException>(() => New(amount));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Initiate_thieu_key_nem_loi(string key)
        => Assert.Throws<DomainValidationException>(() => Payment.Initiate(Guid.NewGuid(), Guid.NewGuid(), 1000m, key));

    [Fact]
    public void Initiate_key_qua_dai_nem_loi()
        => Assert.Throws<DomainValidationException>(
            () => Payment.Initiate(Guid.NewGuid(), Guid.NewGuid(), 1000m, new string('k', 101)));

    [Fact]
    public void MarkSucceeded_ghi_hai_but_toan_can_bang()
    {
        var p = New();
        p.MarkSucceeded("ref");

        Assert.Equal(PaymentStatus.Succeeded, p.Status);
        Assert.Equal(2, p.Entries.Count);
        Assert.Equal(0m, Net(p));
    }

    [Fact]
    public void MarkFailed_khong_ghi_so_cai()
    {
        var p = New();
        p.MarkFailed("The bi tu choi");

        Assert.Equal(PaymentStatus.Failed, p.Status);
        Assert.Empty(p.Entries);
    }

    [Fact]
    public void Refund_dao_but_toan_va_tong_van_can_bang()
    {
        var p = New();
        p.MarkSucceeded("ref");
        p.Refund();

        Assert.Equal(PaymentStatus.Refunded, p.Status);
        Assert.Equal(4, p.Entries.Count);
        Assert.Equal(0m, Net(p));
    }

    [Fact]
    public void Refund_khi_chua_thanh_cong_nem_loi()
        => Assert.Throws<DomainException>(() => New().Refund());

    [Fact]
    public void MarkSucceeded_hai_lan_nem_loi()
    {
        var p = New();
        p.MarkSucceeded("ref");
        Assert.Throws<DomainException>(() => p.MarkSucceeded("ref2"));
    }

        [Fact]
    public void MarkDeferred_giu_chua_ket_thuc_va_khong_ghi_so_cai()
    {
        var p = New();
        p.MarkDeferred("timeout");

        Assert.Equal(PaymentStatus.Deferred, p.Status);
        Assert.Equal("timeout", p.FailureReason);
        Assert.Empty(p.Entries);
    }

    [Fact]
    public void Deferred_van_thanh_cong_duoc_va_xoa_ly_do_loi()
    {
        var p = New();
        p.MarkDeferred("timeout");
        p.MarkSucceeded("ref");

        Assert.Equal(PaymentStatus.Succeeded, p.Status);
        Assert.Null(p.FailureReason);
        Assert.Equal(2, p.Entries.Count);
    }

    [Fact]
    public void Deferred_khong_hoan_tien_duoc()
    {
        var p = New();
        p.MarkDeferred("timeout");
        Assert.Throws<DomainException>(() => p.Refund());
    }

    [Fact]
    public void RecordAttempt_dem_so_lan_thu_va_khong_cho_khi_da_co_ket_qua_cuoi()
    {
        var p = New();
        p.RecordAttempt();
        p.RecordAttempt();
        Assert.Equal(2, p.Attempts);

        p.MarkFailed("x");
        Assert.Throws<DomainException>(() => p.RecordAttempt());
    }

    [Fact]
    public void UseMethod_doi_phuong_thuc_khi_chua_co_ket_qua_cuoi()
    {
        var p = New();
        p.MarkDeferred("timeout");
        p.UseMethod(PaymentMethodKind.Wallet);
        Assert.Equal(PaymentMethodKind.Wallet, p.Method);

        p.MarkSucceeded("ref");
        Assert.Throws<DomainException>(() => p.UseMethod(PaymentMethodKind.Card));
    }
        [Fact]
    public void Refund_ghi_ly_do_va_cat_o_300_ky_tu()
    {
        var p = New();
        p.MarkSucceeded("ref");
        p.Refund(new string('x', 400));

        Assert.Equal(300, p.RefundReason!.Length);
    }

    [Fact]
    public void Refund_khong_truyen_ly_do_van_chay_nhu_cu()
    {
        var p = New();
        p.MarkSucceeded("ref");
        p.Refund();

        Assert.Null(p.RefundReason);
        Assert.Equal(PaymentStatus.Refunded, p.Status);
    }
}