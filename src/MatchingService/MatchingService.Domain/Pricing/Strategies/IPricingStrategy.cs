namespace MatchingService.Domain.Pricing.Strategies;

public interface IPricingStrategy
{
    string Name { get; }
    decimal SurgeMultiplier { get; }     // hệ số s trong công thức
    decimal DiscountMultiplier { get; }  // hệ số η trong công thức
}