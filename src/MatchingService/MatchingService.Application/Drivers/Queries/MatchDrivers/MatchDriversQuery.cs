namespace MatchingService.Application.Drivers.Queries.MatchDrivers;

public record MatchDriversQuery(double? Lat, double? Lng, IReadOnlyList<Guid>? ExcludeDriverIds = null);