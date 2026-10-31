namespace MatchingService.Application.Drivers.Dtos;

public record DriverLocationDto(Guid DriverId, double Latitude, double Longitude, bool IsAvailable, DateTime UpdatedAt);

public record NearbyDriverDto(Guid DriverId, double Latitude, double Longitude, double DistanceKm);