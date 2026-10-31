namespace TripService.Application.Trips.Commands.RequestTrip;

public record RequestTripCommand(
    string Pickup, double? PickupLat, double? PickupLng,
    string Destination, double? DestinationLat, double? DestinationLng,
    string? PromoCode = null);