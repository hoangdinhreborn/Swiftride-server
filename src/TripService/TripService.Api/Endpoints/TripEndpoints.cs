using BuildingBlocks.Security;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Commands.AcceptTrip;
using TripService.Application.Trips.Commands.DropOffTrip;
using TripService.Application.Trips.Commands.PickUpTrip;
using TripService.Application.Trips.Commands.RequestTrip;
using TripService.Application.Trips.Commands.CancelTrip;
using TripService.Application.Trips.Commands.ConfirmTrip;
using TripService.Application.Trips.Commands.DeclineTrip;
using TripService.Application.Trips.Queries.GetMyOffers;
using TripService.Application.Trips.Dtos;
using TripService.Application.Trips.Queries.GetTrip;

namespace TripService.Api.Endpoints;

public static class TripEndpoints
{
    public static IEndpointRouteBuilder MapTripEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/trips");

        g.MapPost("", async (RequestTripCommand cmd, IHandler<RequestTripCommand, TripDto> h, CancellationToken ct) =>
        {
            var trip = await h.HandleAsync(cmd, ct);
            return Results.Created($"/trips/{trip.Id}", trip);
        }).RequireAuthorization(Policies.RiderOnly);

        g.MapGet("/{id:guid}", async (Guid id, IHandler<GetTripQuery, TripDto> h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new GetTripQuery(id), ct)))
            .RequireAuthorization(Policies.AnyRole);

        g.MapPost("/{id:guid}/driver-accept", async (Guid id, IHandler<AcceptTripCommand, TripDto> h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new AcceptTripCommand(id), ct)))
            .RequireAuthorization(Policies.DriverOnly);

        g.MapPost("/{id:guid}/pickup", async (Guid id, IHandler<PickUpTripCommand, TripDto> h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new PickUpTripCommand(id), ct)))
            .RequireAuthorization(Policies.DriverOnly);

        g.MapPost("/{id:guid}/dropoff", async (Guid id, IHandler<DropOffTripCommand, TripDto> h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new DropOffTripCommand(id), ct)))
            .RequireAuthorization(Policies.DriverOnly);

        g.MapPost("/{id:guid}/confirm", async (Guid id, IHandler<ConfirmTripCommand, TripDto> h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new ConfirmTripCommand(id), ct)))
            .RequireAuthorization(Policies.RiderOnly);

        g.MapPost("/{id:guid}/cancel", async (Guid id, IHandler<CancelTripCommand, TripDto> h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new CancelTripCommand(id), ct)))
            .RequireAuthorization(Policies.RiderOnly);

        g.MapPost("/{id:guid}/driver-decline", async (Guid id, IHandler<DeclineTripCommand, TripDto> h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new DeclineTripCommand(id), ct)))
            .RequireAuthorization(Policies.DriverOnly);

        // Các chuyến đang chờ tài xế này trả lời (tài xế gọi định kỳ, hoặc khi nhận thông báo)
        g.MapGet("/offers", async (IHandler<GetMyOffersQuery, IReadOnlyList<TripDto>> h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new GetMyOffersQuery(), ct)))
            .RequireAuthorization(Policies.DriverOnly);

        return app;
    }
}