namespace TripService.Application.Trips.Events;

public record TripAccepted(Guid EventId, Guid TripId, Guid RiderId, Guid DriverId, DateTime OccurredAt);