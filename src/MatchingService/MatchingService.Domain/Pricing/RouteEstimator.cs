using MatchingService.Domain.Geo;

namespace MatchingService.Domain.Pricing;

public static class RouteEstimator
{
    private const decimal DetourFactor = 1.3m;      // đường thật dài hơn đường chim bay
    private const decimal AverageSpeedKmh = 25m;    // tốc độ trung bình trong thành phố

    public static (decimal DistanceKm, decimal Minutes) Estimate(GeoPoint from, GeoPoint to)
    {
        var km = (decimal)from.DistanceKmTo(to) * DetourFactor;
        return (Math.Round(km, 2), Math.Round(km / AverageSpeedKmh * 60m, 1));
    }
}