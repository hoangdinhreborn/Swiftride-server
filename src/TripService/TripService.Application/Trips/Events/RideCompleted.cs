namespace TripService.Application.Trips.Events;

public static class RideEvents
{
    public const string Completed = "ride.completed";
}

public record RideCompleted(
    Guid EventId, Guid TripId, Guid RiderId, Guid DriverId, decimal Amount, DateTime OccurredAt);