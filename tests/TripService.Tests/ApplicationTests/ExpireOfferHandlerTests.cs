using Moq;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips;
using TripService.Application.Trips.Commands.ExpireOffer;
using TripService.Application.Trips.Interfaces;
using TripService.Domain.Trips;
using Xunit;

namespace TripService.Tests.ApplicationTests;

public class ExpireOfferHandlerTests
{
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private readonly Mock<ITripRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDriverMatchingGateway> _matching = new();
    private static readonly OfferExpiryOptions Options = new() { Ttl = TimeSpan.FromSeconds(30) };

    private ExpireOfferHandler Handler(TimeSpan elapsed)
        => new(_repo.Object, _uow.Object, new DriverDispatcher(_matching.Object), Options,
               new FixedClock(DateTimeOffset.UtcNow + elapsed));

    private Trip Stored(Trip t)
    {
        _repo.Setup(r => r.GetByIdAsync(t.Id, It.IsAny<CancellationToken>())).ReturnsAsync(t);
        return t;
    }

    private void MatchReturns(Guid? id)
        => _matching.Setup(m => m.FindNearestDriverAsync(It.IsAny<MatchRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(id);

    [Fact]
    public async Task Het_han_thi_coi_la_tu_choi_va_de_xuat_cho_nguoi_ke()
    {
        var first = Guid.NewGuid(); var second = Guid.NewGuid();
        var trip = Stored(TestTrips.Offered(first));
        MatchReturns(second);

        var handled = await Handler(TimeSpan.FromSeconds(60)).HandleAsync(new ExpireOfferCommand(trip.Id));

        Assert.True(handled);
        Assert.Equal(second, trip.OfferedDriverId);
        Assert.Contains(first, trip.DeclinedDriverIds);
        _matching.Verify(m => m.FindNearestDriverAsync(
            It.Is<MatchRequest>(r => r.ExcludeDriverIds.Contains(first)), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Het_han_ma_khong_con_tai_xe_thi_Failed()
    {
        var trip = Stored(TestTrips.Offered(Guid.NewGuid()));
        MatchReturns(null);

        await Handler(TimeSpan.FromSeconds(60)).HandleAsync(new ExpireOfferCommand(trip.Id));

        Assert.Equal(TripStatus.Failed, trip.Status);
    }

    [Fact]
    public async Task Chua_het_han_thi_khong_lam_gi()
    {
        var trip = Stored(TestTrips.Offered(Guid.NewGuid()));

        var handled = await Handler(TimeSpan.FromSeconds(5)).HandleAsync(new ExpireOfferCommand(trip.Id));

        Assert.False(handled);
        Assert.Equal(TripStatus.DriverOffered, trip.Status);
        _matching.Verify(m => m.FindNearestDriverAsync(It.IsAny<MatchRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Tai_xe_vua_nhan_xong_thi_worker_bo_qua()
    {
        var d = Guid.NewGuid();
        var trip = Stored(TestTrips.Accepted(d));

        var handled = await Handler(TimeSpan.FromMinutes(5)).HandleAsync(new ExpireOfferCommand(trip.Id));

        Assert.False(handled);
        Assert.Equal(TripStatus.DriverAccepted, trip.Status);
    }

    [Fact]
    public async Task Chuyen_khong_ton_tai_thi_bo_qua()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Trip?)null);

        Assert.False(await Handler(TimeSpan.FromMinutes(5)).HandleAsync(new ExpireOfferCommand(Guid.NewGuid())));
    }

    [Fact]
    public async Task Matching_loi_thi_khong_luu_gi_de_luot_sau_thu_lai()
    {
        var first = Guid.NewGuid();
        var trip = Stored(TestTrips.Offered(first));
        _matching.Setup(m => m.FindNearestDriverAsync(It.IsAny<MatchRequest>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new TripService.Application.Common.Exceptions.PricingUnavailableException("down"));

        await Assert.ThrowsAsync<TripService.Application.Common.Exceptions.PricingUnavailableException>(
            () => Handler(TimeSpan.FromSeconds(60)).HandleAsync(new ExpireOfferCommand(trip.Id)));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}