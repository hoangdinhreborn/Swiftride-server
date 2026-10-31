using BuildingBlocks.Security;
using TripService.Application.Common.Exceptions;
using TripService.Application.Common.Interfaces;
using TripService.Application.Common.Telemetry;
using TripService.Application.Trips.Dtos;
using TripService.Application.Trips.Interfaces;
using TripService.Domain.Common;
using TripService.Domain.Trips;

namespace TripService.Application.Trips.Commands.RequestTrip;

public sealed class RequestTripHandler(
    ITripRepository repo, IUnitOfWork uow, ICurrentUser user, IPricingGateway pricing)
    : IHandler<RequestTripCommand, TripDto>
{
    public async Task<TripDto> HandleAsync(RequestTripCommand cmd, CancellationToken ct = default)
    {
        try
        {
            var dto = await CreateAsync(cmd, ct);
            TripMetrics.TripRequested("ok");
            return dto;
        }
        catch (PricingUnavailableException) { TripMetrics.TripRequested("pricing_unavailable"); throw; }
        catch (DomainValidationException)   { TripMetrics.TripRequested("invalid"); throw; }
        catch                               { TripMetrics.TripRequested("error"); throw; }
    }

    private async Task<TripDto> CreateAsync(RequestTripCommand cmd, CancellationToken ct)
    {
        if (cmd.PickupLat is not { } pLat || cmd.PickupLng is not { } pLng ||
            cmd.DestinationLat is not { } dLat || cmd.DestinationLng is not { } dLng)
            throw new DomainValidationException("Thiếu tọa độ điểm đón hoặc điểm đến");

        var quote = await pricing.GetQuoteAsync(new QuoteRequest(pLat, pLng, dLat, dLng, cmd.PromoCode), ct);

        var trip = Trip.Request(user.Id, cmd.Pickup, pLat, pLng, cmd.Destination, dLat, dLng, quote.Total);
        await repo.AddAsync(trip, ct);
        await uow.SaveChangesAsync(ct);
        return trip.ToDto();
    }
}