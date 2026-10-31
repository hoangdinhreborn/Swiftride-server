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
}