namespace TripService.Application.Trips.Dtos;

public record TripDto(Guid Id, Guid RiderId, Guid? DriverId, string Pickup, string Destination, string Status, DateTime CreatedAt);