using BuildingBlocks.Security;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Payments.Commands.CreatePayment;
using PaymentService.Application.Payments.Commands.RefundPayment;
using PaymentService.Application.Payments.Dtos;
using PaymentService.Application.Payments.Queries.GetPayment;

namespace PaymentService.Api.Endpoints;

public record CreatePaymentRequest(Guid? TripId, string? PaymentToken);

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/payments");

        g.MapPost("", async (
                CreatePaymentRequest req,
                [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
                IHandler<CreatePaymentCommand, CreatePaymentResult> h,
                CancellationToken ct) =>
            {
                var r = await h.HandleAsync(new CreatePaymentCommand(req.TripId, req.PaymentToken, idempotencyKey), ct);

                if (r.Payment.Status == "Failed") return Results.Json(r.Payment, statusCode: 402);   // thẻ bị từ chối
                if (r.IsReplay) return Results.Ok(r.Payment);
                return Results.Created($"/payments/{r.Payment.Id}", r.Payment);
            })
            .RequireAuthorization(Policies.RiderOnly);

        g.MapGet("/{id:guid}", async (Guid id, IHandler<GetPaymentQuery, PaymentDto> h, CancellationToken ct) =>
                Results.Ok(await h.HandleAsync(new GetPaymentQuery(id), ct)))
            .RequireAuthorization(Policies.AnyRole);

        g.MapPost("/{id:guid}/refund", async (Guid id, IHandler<RefundPaymentCommand, PaymentDto> h, CancellationToken ct) =>
                Results.Ok(await h.HandleAsync(new RefundPaymentCommand(id), ct)))
            .RequireAuthorization(p => p.RequireRole(Roles.Admin));

        return app;
    }
}