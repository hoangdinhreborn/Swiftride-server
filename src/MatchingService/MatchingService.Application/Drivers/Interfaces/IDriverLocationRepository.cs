using MatchingService.Application.Drivers.Dtos;
using MatchingService.Domain.Drivers;
using MatchingService.Domain.Geo;

namespace MatchingService.Application.Drivers.Interfaces;

public interface IDriverLocationRepository
{
    Task UpsertAsync(DriverLocation driver, CancellationToken ct = default);

    // Kết quả đã sắp theo khoảng cách tăng dần
    Task<IReadOnlyList<NearbyDriverDto>> FindNearestAvailableAsync(
        GeoPoint from, double maxDistanceKm, TimeSpan maxAge, int limit, CancellationToken ct = default);
}