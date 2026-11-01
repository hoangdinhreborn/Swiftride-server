using MatchingService.Application.Drivers.Dtos;
using MatchingService.Application.Drivers.Interfaces;
using MatchingService.Domain.Drivers;
using MatchingService.Domain.Geo;
using MatchingService.Infrastructure.Persistence.Documents;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

namespace MatchingService.Infrastructure.Persistence.Repositories;

public class DriverLocationRepository(MatchingMongoContext ctx) : IDriverLocationRepository
{
    public Task UpsertAsync(DriverLocation driver, CancellationToken ct = default)
    {
        var doc = new DriverLocationDocument
        {
            DriverId = driver.DriverId,
            Location = ToGeoJson(driver.Location),
            IsAvailable = driver.IsAvailable,
            UpdatedAt = driver.UpdatedAt
        };

        return ctx.DriverLocations.ReplaceOneAsync(
            x => x.DriverId == driver.DriverId, doc, new ReplaceOptions { IsUpsert = true }, ct);
    }

    public async Task<IReadOnlyList<NearbyDriverDto>> FindNearestAvailableAsync(
    GeoPoint from, double maxDistanceKm, TimeSpan maxAge, int limit,
    CancellationToken ct = default, IReadOnlyCollection<Guid>? exclude = null)
    {
        var f = Builders<DriverLocationDocument>.Filter;
        var filters = new List<FilterDefinition<DriverLocationDocument>>
        {
            f.Eq(x => x.IsAvailable, true),
            f.Gte(x => x.UpdatedAt, DateTime.UtcNow - maxAge),
            f.Near(x => x.Location, ToGeoJson(from), maxDistance: maxDistanceKm * 1000)   // mét
        };
        if (exclude is { Count: > 0 }) filters.Add(f.Nin(x => x.DriverId, exclude));

        // $near tự sắp theo khoảng cách tăng dần
        var docs = await ctx.DriverLocations.Find(f.And(filters)).Limit(limit).ToListAsync(ct);

        return docs.Select(d =>
        {
            var p = new GeoPoint(d.Location.Coordinates.Latitude, d.Location.Coordinates.Longitude);
            return new NearbyDriverDto(d.DriverId, p.Latitude, p.Longitude, Math.Round(p.DistanceKmTo(from), 2));
        }).ToList();
    }

    // GeoJSON luôn là (kinh độ, vĩ độ): ngược với cách ta hay viết
    private static GeoJsonPoint<GeoJson2DGeographicCoordinates> ToGeoJson(GeoPoint p)
        => new(new GeoJson2DGeographicCoordinates(p.Longitude, p.Latitude));
}