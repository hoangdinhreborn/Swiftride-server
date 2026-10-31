namespace MatchingService.Application.Drivers.Commands.UpdateDriverLocation;

public record UpdateDriverLocationCommand(double? Lat, double? Lng, bool IsAvailable);