using BuildingBlocks.Security;
using MatchingService.Application.Common.Interfaces;
using MatchingService.Application.Pricing.Dtos;
using MatchingService.Application.Pricing.Queries.GetQuote;

namespace MatchingService.Api.Endpoints;

public static class PricingEndpoints
{
    public static IEndpointRouteBuilder MapPricingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/quote",
            async (GetQuoteQuery q, IHandler<GetQuoteQuery, QuoteDto> h, CancellationToken ct) =>
                Results.Ok(await h.HandleAsync(q, ct)))
            .RequireAuthorization(Policies.RiderOnly);

        return app;
    }
}