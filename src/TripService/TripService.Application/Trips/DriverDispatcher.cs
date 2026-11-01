using TripService.Application.Trips.Interfaces;
using TripService.Domain.Trips;

namespace TripService.Application.Trips;

public sealed class DriverDispatcher(IDriverMatchingGateway matching)
{
    // Trip đang Searching: đề xuất cho tài xế gần nhất chưa từ chối, hoặc kết thúc nếu không còn ai
    public async Task DispatchAsync(Trip trip, CancellationToken ct)
    {
        var driverId = await matching.FindNearestDriverAsync(
            new MatchRequest(trip.PickupLat, trip.PickupLng, trip.DeclinedDriverIds), ct);

        if (driverId is { } id) trip.OfferTo(id);
        else trip.NoDriverFound();
    }
}