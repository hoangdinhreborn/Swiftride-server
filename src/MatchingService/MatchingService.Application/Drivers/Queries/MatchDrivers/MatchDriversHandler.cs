using MatchingService.Application.Common.Interfaces;
using MatchingService.Application.Drivers.Dtos;
using MatchingService.Application.Drivers.Interfaces;
using MatchingService.Domain.Geo;

namespace MatchingService.Application.Drivers.Queries.MatchDrivers;

public sealed class MatchDriversHandler(IDriverLocationRepository repo)
    : IHandler<MatchDriversQuery, IReadOnlyList<NearbyDriverDto>>
{
    public async Task<IReadOnlyList<NearbyDriverDto>> HandleAsync(MatchDriversQuery q, CancellationToken ct = default)
    {
        var pickup = GeoPoint.From(q.Lat, q.Lng, "điểm đón");

        return await repo.FindNearestAvailableAsync(
            pickup, MatchingPolicy.SearchRadiusKm, MatchingPolicy.MaxLocationAge, MatchingPolicy.MaxCandidates, ct);
    }
}