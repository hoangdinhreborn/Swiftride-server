namespace TripService.Application.Trips.Interfaces;

public record QuoteRequest(double PickupLat, double PickupLng, double DestinationLat, double DestinationLng, string? PromoCode);

public record QuoteResult(decimal Total, decimal DistanceKm, decimal EstimatedMinutes, string Pricing);

public interface IPricingGateway
{
    Task<QuoteResult> GetQuoteAsync(QuoteRequest request, CancellationToken ct = default);
}