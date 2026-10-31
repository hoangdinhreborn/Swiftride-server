using MatchingService.Domain.Common;
using MatchingService.Domain.Pricing.Strategies;

namespace MatchingService.Domain.Pricing;

public sealed class FareCalculator(FareRates rates)
{
    public FareBreakdown Calculate(FareInput input, IPricingStrategy strategy)
    {
        if (input.DistanceKm < 0 || input.Minutes < 0 || input.Toll < 0)
            throw new DomainValidationException("Quãng đường, thời gian và phí phụ không được âm");

        // p = ((cd·d + ct·t)·s + cm)·η·(1 + T)
        var p = ((rates.PerKm * input.DistanceKm + rates.PerMinute * input.Minutes) * strategy.SurgeMultiplier
                 + input.Toll)
                * strategy.DiscountMultiplier
                * (1m + rates.TaxRate);

        var total = Math.Max(RoundToThousand(p), rates.MinimumFare);

        return new FareBreakdown(input.DistanceKm, input.Minutes, strategy.Name,
            strategy.SurgeMultiplier, strategy.DiscountMultiplier, input.Toll, rates.TaxRate, total);
    }

    private static decimal RoundToThousand(decimal v)
        => Math.Round(v / 1000m, 0, MidpointRounding.AwayFromZero) * 1000m;
}