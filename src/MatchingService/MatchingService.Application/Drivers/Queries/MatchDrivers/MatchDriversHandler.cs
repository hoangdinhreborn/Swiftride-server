using System.Diagnostics;
using MatchingService.Application.Common.Interfaces;
using MatchingService.Application.Common.Telemetry;
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

        var sw = Stopwatch.StartNew();
        var result = await repo.FindNearestAvailableAsync(
            pickup, MatchingPolicy.SearchRadiusKm, MatchingPolicy.MaxLocationAge, MatchingPolicy.MaxCandidates, ct, q.ExcludeDriverIds);
        MatchingMetrics.Record(sw.Elapsed.TotalMilliseconds, found: result.Count > 0);

        return result;
    }
}