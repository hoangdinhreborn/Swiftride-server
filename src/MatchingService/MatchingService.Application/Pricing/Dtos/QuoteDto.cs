namespace MatchingService.Application.Pricing.Dtos;

public record QuoteDto(
    decimal DistanceKm, decimal EstimatedMinutes, string Pricing,
    decimal SurgeMultiplier, decimal DiscountMultiplier, decimal TaxRate,
    decimal Total, string Currency);