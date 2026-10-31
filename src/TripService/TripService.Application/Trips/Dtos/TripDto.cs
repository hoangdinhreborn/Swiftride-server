namespace TripService.Application.Trips.Dtos;

public record TripDto(
    Guid Id, Guid RiderId, Guid? DriverId,
    string Pickup, string Destination,
    decimal QuotedFare,   // VND
    string Status, DateTime CreatedAt);