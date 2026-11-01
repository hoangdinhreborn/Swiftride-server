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

    // Chuyến đã xác nhận giá và đề xuất cho một tài xế
    public static Trip Offered(Guid driverId, Guid? riderId = null, decimal fare = 85_000m)
    {
        var t = Make(riderId, fare: fare);
        t.Confirm();
        t.OfferTo(driverId);
        return t;
    }

    // Chuyến mà tài xế đã nhận
    public static Trip Accepted(Guid? driverId = null, decimal fare = 85_000m)
    {
        var d = driverId ?? Guid.NewGuid();
        var t = Offered(d, fare: fare);
        t.AcceptBy(d);
        return t;
    }
}