namespace MatchingService.Application.Pricing.Queries.GetQuote;

public record GetQuoteQuery(
    double? PickupLat, double? PickupLng,
    double? DestinationLat, double? DestinationLng,
    string? PromoCode);