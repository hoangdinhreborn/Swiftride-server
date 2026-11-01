using PaymentService.Domain.Common;
using PaymentService.Domain.Payments;
using Xunit;

namespace PaymentService.Tests;

public class PaymentMethodTests
{
    [Theory]
    [InlineData(null, "tok_abc", PaymentMethodKind.Card)]
    [InlineData("", "tok_abc", PaymentMethodKind.Card)]
    [InlineData("card", "tok_abc", PaymentMethodKind.Card)]
    [InlineData("Wallet", "wallet_abc", PaymentMethodKind.Wallet)]
    [InlineData("  WALLET ", "wallet_abc", PaymentMethodKind.Wallet)]
    public void Factory_chon_dung_loai_phuong_thuc(string? kind, string token, PaymentMethodKind expected)
    {
        var m = PaymentMethodFactory.Create(kind, token);

        Assert.Equal(expected, m.Kind);
        Assert.Equal(token, m.Token);
    }

    [Theory]
    [InlineData("Bitcoin", "tok_abc")]            // loại không hỗ trợ
    [InlineData("5", "tok_abc")]                  // số của enum không được phép lọt qua
    [InlineData("Card", "")]
    [InlineData("Card", null)]
    [InlineData("Card", "4111111111111111")]      // số thẻ thật bị từ chối
    [InlineData("Card", "wallet_abc")]            // token sai loại
    [InlineData("Wallet", "tok_abc")]
    public void Factory_tu_choi_du_lieu_sai(string? kind, string? token)
        => Assert.Throws<DomainValidationException>(() => PaymentMethodFactory.Create(kind, token));

    [Fact]
    public void Token_qua_dai_bi_tu_choi()
        => Assert.Throws<DomainValidationException>(
            () => PaymentMethodFactory.Create("Card", "tok_" + new string('a', 100)));

    [Fact]
    public void Payment_luu_loai_phuong_thuc_va_mac_dinh_la_Card()
    {
        Assert.Equal(PaymentMethodKind.Card, Payment.Initiate(Guid.NewGuid(), Guid.NewGuid(), 1000m, "k").Method);
        Assert.Equal(PaymentMethodKind.Wallet,
            Payment.Initiate(Guid.NewGuid(), Guid.NewGuid(), 1000m, "k", PaymentMethodKind.Wallet).Method);
    }
}