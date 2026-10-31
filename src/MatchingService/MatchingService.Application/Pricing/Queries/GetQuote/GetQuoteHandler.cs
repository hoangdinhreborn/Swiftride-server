using MatchingService.Application.Common.Interfaces;
using MatchingService.Application.Drivers;
using MatchingService.Application.Drivers.Interfaces;
using MatchingService.Application.Pricing.Dtos;
using MatchingService.Domain.Geo;
using MatchingService.Domain.Pricing;

namespace MatchingService.Application.Pricing.Queries.GetQuote;

public sealed class GetQuoteHandler(IDriverLocationRepository drivers, FareCalculator calculator)
    : IHandler<GetQuoteQuery, QuoteDto>
{
    public async Task<QuoteDto> HandleAsync(GetQuoteQuery q, CancellationToken ct = default)
    {
        var pickup = GeoPoint.From(q.PickupLat, q.PickupLng, "điểm đón");
        var destination = GeoPoint.From(q.DestinationLat, q.DestinationLng, "điểm đến");

        var (km, minutes) = RouteEstimator.Estimate(pickup, destination);

        // Ít tài xế rảnh quanh điểm đón → surge cao hơn
        var nearby = await drivers.FindNearestAvailableAsync(
            pickup, MatchingPolicy.SurgeRadiusKm, MatchingPolicy.MaxLocationAge, limit: 3, ct);

        var strategy = PricingStrategyFactory.Create(nearby.Count, q.PromoCode);

        // Toll = 0 vì chưa có Toll Service
        var fare = calculator.Calculate(new FareInput(km, minutes), strategy);

        return new QuoteDto(fare.DistanceKm, fare.Minutes, fare.Strategy,
            fare.Surge, fare.Discount, fare.TaxRate, fare.Total, "VND");
    }
}