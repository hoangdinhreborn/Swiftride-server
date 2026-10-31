using MatchingService.Domain.Common;
using MatchingService.Domain.Geo;
using MatchingService.Domain.Pricing;
using Xunit;

namespace MatchingService.Tests;

public class PricingAndGeoTests
{
    private readonly FareCalculator _calc = new(new FareRates());

    [Fact]
    public void Standard_10km_20phut_la_108000()
    {
        var fare = _calc.Calculate(new FareInput(10m, 20m), PricingStrategyFactory.Create(5, null));
        Assert.Equal(108_000m, fare.Total);   // (90.000 + 10.000) × 1 × 1,08
    }

    [Fact]
    public void Khong_con_tai_xe_nao_thi_surge_1_8()
    {
        var fare = _calc.Calculate(new FareInput(10m, 20m), PricingStrategyFactory.Create(0, null));
        Assert.Equal(194_000m, fare.Total);   // 100.000 × 1,8 × 1,08 = 194.400 → làm tròn nghìn
    }

    [Fact]
    public void Promo_SWIFT10_giam_10_phan_tram()
    {
        var fare = _calc.Calculate(new FareInput(10m, 20m), PricingStrategyFactory.Create(5, "swift10"));
        Assert.Equal(97_000m, fare.Total);    // 100.000 × 0,9 × 1,08 = 97.200
    }

    [Fact]
    public void Phi_phu_toll_duoc_cong_truoc_khi_nhan_thue()
    {
        var fare = _calc.Calculate(new FareInput(10m, 20m, 20_000m), PricingStrategyFactory.Create(5, null));
        Assert.Equal(130_000m, fare.Total);   // (100.000 + 20.000) × 1,08 = 129.600
    }

    [Fact]
    public void Chuyen_qua_ngan_van_tinh_gia_toi_thieu()
    {
        var fare = _calc.Calculate(new FareInput(0.5m, 1m), PricingStrategyFactory.Create(5, null));
        Assert.Equal(15_000m, fare.Total);
    }

    [Theory]
    [InlineData(5, 1.0)]
    [InlineData(3, 1.0)]
    [InlineData(2, 1.3)]
    [InlineData(1, 1.3)]
    [InlineData(0, 1.8)]
    public void He_so_surge_theo_so_tai_xe(int drivers, double expected)
        => Assert.Equal((decimal)expected, PricingStrategyFactory.Create(drivers, null).SurgeMultiplier);

    [Fact]
    public void Ma_khuyen_mai_sai_nem_DomainValidationException()
        => Assert.Throws<DomainValidationException>(() => PricingStrategyFactory.Create(5, "XYZ"));

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(double.NaN, 0)]
    public void Toa_do_sai_nem_DomainValidationException(double lat, double lng)
        => Assert.Throws<DomainValidationException>(() => new GeoPoint(lat, lng));

    [Fact]
    public void Khoang_cach_BenThanh_PhuMyHung_khoang_5km()
    {
        var d = new GeoPoint(10.7721, 106.6980).DistanceKmTo(new GeoPoint(10.7290, 106.7190));
        Assert.InRange(d, 4.5, 6.0);
    }
}