using TripService.Domain.Trips;

namespace TripService.Tests;

internal static class TestTrips
{
    public static Trip Make(
        Guid? riderId = null,
        string pickup = "Q1", double pickupLat = 10.7721, double pickupLng = 106.6980,
        string destination = "Q7", double destinationLat = 10.7290, double destinationLng = 106.7190,
        decimal fare = 85_000m)
        => Trip.Request(riderId ?? Guid.NewGuid(), pickup, pickupLat, pickupLng,
                        destination, destinationLat, destinationLng, fare);
}
