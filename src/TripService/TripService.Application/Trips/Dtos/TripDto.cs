namespace TripService.Application.Trips.Dtos;

public record TripDto(
    Guid Id, Guid RiderId, Guid? DriverId,
    string Pickup, double PickupLat, double PickupLng,
    string Destination, double DestinationLat, double DestinationLng,
    decimal QuotedFare,
    string Status, DateTime CreatedAt);
