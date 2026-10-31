using TripService.Domain.Trips;

namespace TripService.Application.Trips.Dtos;

public static class TripMappings
{
    public static TripDto ToDto(this Trip t)
        => new(t.Id, t.RiderId, t.DriverId, t.Pickup, t.Destination, t.Status.ToString(), t.CreatedAt);
}