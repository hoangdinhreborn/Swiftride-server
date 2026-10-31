namespace MatchingService.Domain.Pricing;

// Đơn giá (VND). Có thể ghi đè bằng mục "Fare" trong appsettings.json
public sealed class FareRates
{
    public decimal PerKm { get; init; } = 9_000m;       // cd
    public decimal PerMinute { get; init; } = 500m;     // ct
    public decimal TaxRate { get; init; } = 0.08m;      // T
    public decimal MinimumFare { get; init; } = 15_000m;
}

public sealed record FareInput(decimal DistanceKm, decimal Minutes, decimal Toll = 0m); // Toll = cm

public sealed record FareBreakdown(
    decimal DistanceKm, decimal Minutes, string Strategy,
    decimal Surge, decimal Discount, decimal Toll, decimal TaxRate, decimal Total);