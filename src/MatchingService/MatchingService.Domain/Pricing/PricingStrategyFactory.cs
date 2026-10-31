using MatchingService.Domain.Common;
using MatchingService.Domain.Pricing.Strategies;

namespace MatchingService.Domain.Pricing;

public static class PricingStrategyFactory
{
    public static IPricingStrategy Create(int availableDriversNearby, string? promoCode)
    {
        IPricingStrategy strategy = availableDriversNearby switch
        {
            >= 3 => new StandardPricing(),
            >= 1 => new SurgePricing(1.3m),
            _    => new SurgePricing(1.8m)
        };

        if (string.IsNullOrWhiteSpace(promoCode)) return strategy;

        // Demo: mã cứng. Thực tế lấy từ DB/dịch vụ khuyến mãi
        return promoCode.Trim().ToUpperInvariant() switch
        {
            "SWIFT10" => new PromoPricing(strategy, 0.10m),
            _ => throw new DomainValidationException("Mã khuyến mãi không hợp lệ")
        };
    }
}