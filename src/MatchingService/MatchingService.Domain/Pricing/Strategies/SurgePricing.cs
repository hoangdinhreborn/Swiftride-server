using MatchingService.Domain.Common;

namespace MatchingService.Domain.Pricing.Strategies;

public sealed class SurgePricing : IPricingStrategy
{
    public SurgePricing(decimal multiplier)
    {
        if (multiplier < 1m)
            throw new DomainValidationException("Hệ số surge phải từ 1 trở lên");
        SurgeMultiplier = multiplier;
    }

    public string Name => "Surge";
    public decimal SurgeMultiplier { get; }
    public decimal DiscountMultiplier => 1m;
}