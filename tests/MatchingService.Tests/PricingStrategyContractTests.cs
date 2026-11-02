using MatchingService.Domain.Pricing;
using MatchingService.Domain.Pricing.Strategies;
using Xunit;

namespace MatchingService.Tests;

// mọi IPricingStrategy phải thay thế được nhau mà không phá hành vi mong đợi.
// Hợp đồng: hệ số hợp lệ, và giá không bao giờ âm
public class PricingStrategyContractTests
{
    public static IEnumerable<object[]> Strategies()
    {
        yield return new object[] { new StandardPricing() };
        yield return new object[] { new SurgePricing(1m) };
        yield return new object[] { new SurgePricing(1.3m) };
        yield return new object[] { new SurgePricing(1.8m) };
        yield return new object[] { new SurgePricing(5m) };
        yield return new object[] { new PromoPricing(new StandardPricing(), 0.10m) };
        yield return new object[] { new PromoPricing(new SurgePricing(1.8m), 0.50m) };
        yield return new object[] { new PromoPricing(new PromoPricing(new StandardPricing(), 0.10m), 0.10m) };   // decorator lồng nhau
    }

    private readonly FareCalculator _calc = new(new FareRates());

    [Fact]
    public void Moi_chien_luoc_trong_Domain_deu_nam_trong_danh_sach_kiem_thu()
    {
        var implemented = typeof(IPricingStrategy).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IPricingStrategy).IsAssignableFrom(t))
            .ToHashSet();
        var covered = Strategies().Select(a => a[0].GetType()).ToHashSet();

        Assert.Empty(implemented.Except(covered));   // fail nghĩa là có strategy mới chưa được thêm vào Strategies()
    }

    [Theory]
    [MemberData(nameof(Strategies))]
    public void He_so_hop_le(IPricingStrategy s)
    {
        Assert.False(string.IsNullOrWhiteSpace(s.Name));
        Assert.True(s.SurgeMultiplier >= 1m, "surge phải từ 1 trở lên");
        Assert.InRange(s.DiscountMultiplier, 0.0001m, 1m);   // khuyến mãi chỉ giảm, không làm giá âm hay tăng
    }

    [Theory]
    [MemberData(nameof(Strategies))]
    public void Gia_khong_am_va_khong_thap_hon_gia_toi_thieu(IPricingStrategy s)
    {
        foreach (var km in new[] { 0m, 0.1m, 5m, 50m })
        foreach (var minutes in new[] { 0m, 1m, 90m })
        foreach (var toll in new[] { 0m, 20_000m })
        {
            var fare = _calc.Calculate(new FareInput(km, minutes, toll), s);
            Assert.True(fare.Total >= 0m);
            Assert.True(fare.Total >= new FareRates().MinimumFare);
        }
    }

    [Theory]
    [MemberData(nameof(Strategies))]
    public void Gia_khong_giam_khi_quang_duong_tang(IPricingStrategy s)
    {
        var previous = 0m;
        foreach (var km in new[] { 0m, 1m, 5m, 10m, 30m, 100m })
        {
            var total = _calc.Calculate(new FareInput(km, km * 2), s).Total;
            Assert.True(total >= previous);
            previous = total;
        }
    }
}