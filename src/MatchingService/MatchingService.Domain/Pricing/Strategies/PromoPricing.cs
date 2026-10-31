using MatchingService.Domain.Common;

namespace MatchingService.Domain.Pricing.Strategies;

public sealed class PromoPricing : IPricingStrategy
{
    private readonly IPricingStrategy _inner;

    public PromoPricing(IPricingStrategy inner, decimal percentOff)
    {
        if (percentOff <= 0m || percentOff >= 1m)
            throw new DomainValidationException("Mức giảm phải trong khoảng 0 đến 1 (0.1 = giảm 10%)");

        _inner = inner;
        DiscountMultiplier = inner.DiscountMultiplier * (1m - percentOff);
    }

    public string Name => $"{_inner.Name}+Promo";
    public decimal SurgeMultiplier => _inner.SurgeMultiplier;
    public decimal DiscountMultiplier { get; }
}