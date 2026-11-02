using BuildingBlocks.Security;
using MatchingService.Application.Common.Interfaces;
using MatchingService.Application.Drivers.Commands.UpdateDriverLocation;
using MatchingService.Application.Drivers.Dtos;
using MatchingService.Application.Drivers.Interfaces;
using MatchingService.Application.Drivers.Queries.MatchDrivers;

namespace MatchingService.Api.Endpoints;

public static class DriverEndpoints
{
    public static IEndpointRouteBuilder MapDriverEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/drivers/me/location",
            async (UpdateDriverLocationCommand cmd,
                   IHandler<UpdateDriverLocationCommand, DriverLocationDto> h, CancellationToken ct) =>
                Results.Ok(await h.HandleAsync(cmd, ct)))
            .RequireAuthorization(Policies.DriverOnly);

        app.MapGet("/drivers/{id:guid}/location",
            async (Guid id, IDriverLocationRepository repo, CancellationToken ct) =>
            {
                var d = await repo.GetByDriverIdAsync(id, ct);
                return d is null ? Results.NotFound() : Results.Ok(new DriverLocationDto(d.DriverId, d.Location.Latitude, d.Location.Longitude, d.IsAvailable, d.UpdatedAt));
            })
            .RequireAuthorization(Policies.AnyRole);

        app.MapPost("/match",
            async (MatchDriversQuery q,
                   IHandler<MatchDriversQuery, IReadOnlyList<NearbyDriverDto>> h, CancellationToken ct) =>
                Results.Ok(await h.HandleAsync(q, ct)))
            .RequireAuthorization(Policies.AnyRole);

        return app;
    }
}
