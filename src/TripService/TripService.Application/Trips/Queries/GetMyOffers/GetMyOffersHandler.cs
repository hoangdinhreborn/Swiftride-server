using BuildingBlocks.Security;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Dtos;
using TripService.Application.Trips.Interfaces;

namespace TripService.Application.Trips.Queries.GetMyOffers;

public sealed class GetMyOffersHandler(ITripRepository repo, ICurrentUser user)
    : IHandler<GetMyOffersQuery, IReadOnlyList<TripDto>>
{
    public async Task<IReadOnlyList<TripDto>> HandleAsync(GetMyOffersQuery q, CancellationToken ct = default)
        => (await repo.GetOffersForDriverAsync(user.Id, ct)).Select(t => t.ToDto()).ToList();
}