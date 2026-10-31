using BuildingBlocks.Security;
using Moq;
using TripService.Application.Common.Exceptions;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Commands.PickUpTrip;
using TripService.Application.Trips.Commands.RequestTrip;
using TripService.Application.Trips.Interfaces;
using TripService.Domain.Common;
using TripService.Domain.Trips;
using Xunit;

namespace TripService.Tests.ApplicationTests;

public class HandlerTests
{
    private readonly Mock<ITripRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IPricingGateway> _pricing = new();

    private void LoginAs(Guid id) => _user.Setup(u => u.Id).Returns(id);

    private void QuoteReturns(decimal total)
        => _pricing.Setup(p => p.GetQuoteAsync(It.IsAny<QuoteRequest>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new QuoteResult(total, 6.9m, 16.6m, "Standard"));

    private RequestTripHandler NewRequestHandler()
        => new(_repo.Object, _uow.Object, _user.Object, _pricing.Object);

    private static RequestTripCommand Cmd(string pickup = "Q1", string dest = "Q7", double? pickupLat = 10.7721)
        => new(pickup, pickupLat, 106.6980, dest, 10.7290, 106.7190, null);

    private Trip AcceptedTripBy(Guid driverId)
    {
        var trip = TestTrips.Make();
        trip.AcceptBy(driverId);
        _repo.Setup(r => r.GetByIdAsync(trip.Id, It.IsAny<CancellationToken>())).ReturnsAsync(trip);
        return trip;
    }

    // ---------- RequestTrip ----------

    [Fact]
    public async Task RequestTrip_hop_le_luu_gia_tu_gateway_va_lay_RiderId_tu_token()
    {
        var riderId = Guid.NewGuid();
        LoginAs(riderId);
        QuoteReturns(85_000m);

        var dto = await NewRequestHandler().HandleAsync(Cmd());

        Assert.Equal(riderId, dto.RiderId);
        Assert.Equal(85_000m, dto.QuotedFare);
        Assert.Equal("Requested", dto.Status);
        _repo.Verify(r => r.AddAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("", "Q7")]
    [InlineData("Q1", "  ")]
    public async Task RequestTrip_dia_diem_rong_nem_DomainValidationException_va_khong_luu(string pickup, string dest)
    {
        LoginAs(Guid.NewGuid());
        QuoteReturns(85_000m);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => NewRequestHandler().HandleAsync(Cmd(pickup, dest)));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestTrip_thieu_toa_do_khong_goi_gateway()
    {
        LoginAs(Guid.NewGuid());

        await Assert.ThrowsAsync<DomainValidationException>(
            () => NewRequestHandler().HandleAsync(Cmd(pickupLat: null)));

        _pricing.Verify(p => p.GetQuoteAsync(It.IsAny<QuoteRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestTrip_bao_gia_loi_thi_khong_tao_chuyen()
    {
        LoginAs(Guid.NewGuid());
        _pricing.Setup(p => p.GetQuoteAsync(It.IsAny<QuoteRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new PricingUnavailableException("down"));

        await Assert.ThrowsAsync<PricingUnavailableException>(() => NewRequestHandler().HandleAsync(Cmd()));

        _repo.Verify(r => r.AddAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- PickUpTrip ----------

    [Fact]
    public async Task PickUp_dung_tai_xe_chuyen_sang_PickedUp_va_luu()
    {
        var driverId = Guid.NewGuid();
        var trip = AcceptedTripBy(driverId);
        LoginAs(driverId);
        var handler = new PickUpTripHandler(_repo.Object, _uow.Object, _user.Object);

        var dto = await handler.HandleAsync(new PickUpTripCommand(trip.Id));

        Assert.Equal("PickedUp", dto.Status);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PickUp_sai_tai_xe_nem_ForbiddenException_va_khong_luu()
    {
        var trip = AcceptedTripBy(Guid.NewGuid());
        LoginAs(Guid.NewGuid());
        var handler = new PickUpTripHandler(_repo.Object, _uow.Object, _user.Object);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.HandleAsync(new PickUpTripCommand(trip.Id)));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PickUp_chuyen_khong_ton_tai_nem_NotFoundException()
    {
        LoginAs(Guid.NewGuid());
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Trip?)null);
        var handler = new PickUpTripHandler(_repo.Object, _uow.Object, _user.Object);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.HandleAsync(new PickUpTripCommand(Guid.NewGuid())));
    }
}