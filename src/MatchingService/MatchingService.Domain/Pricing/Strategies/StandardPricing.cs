namespace MatchingService.Domain.Pricing.Strategies;

public sealed class StandardPricing : IPricingStrategy
{
    public string Name => "Standard";
    public decimal SurgeMultiplier => 1m;
    public decimal DiscountMultiplier => 1m;
}