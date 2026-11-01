using BuildingBlocks.Security;
using Moq;
using TripService.Application.Common.Exceptions;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips;
using TripService.Application.Trips.Commands.AcceptTrip;
using TripService.Application.Trips.Commands.CancelTrip;
using TripService.Application.Trips.Commands.ConfirmTrip;
using TripService.Application.Trips.Commands.DeclineTrip;
using TripService.Application.Trips.Interfaces;
using TripService.Domain.Trips;
using Xunit;

namespace TripService.Tests.ApplicationTests;

public class DispatchHandlerTests
{
    private readonly Mock<ITripRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IDriverMatchingGateway> _matching = new();

    private DriverDispatcher Dispatcher() => new(_matching.Object);

    private void LoginAs(Guid id) => _user.Setup(u => u.Id).Returns(id);

    private Trip Stored(Trip t)
    {
        _repo.Setup(r => r.GetByIdAsync(t.Id, It.IsAny<CancellationToken>())).ReturnsAsync(t);
        return t;
    }

    private void MatchReturns(Guid? driverId)
        => _matching.Setup(m => m.FindNearestDriverAsync(It.IsAny<MatchRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(driverId);

    private void SaveNever() => _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    // ---------- Confirm ----------

    [Fact]
    public async Task Confirm_de_xuat_cho_tai_xe_gan_nhat_va_luu_mot_lan()
    {
        var rider = Guid.NewGuid(); var driver = Guid.NewGuid();
        var trip = Stored(TestTrips.Make(rider));
        LoginAs(rider); MatchReturns(driver);

        var dto = await new ConfirmTripHandler(_repo.Object, _uow.Object, _user.Object, Dispatcher())
            .HandleAsync(new ConfirmTripCommand(trip.Id));

        Assert.Equal("DriverOffered", dto.Status);
        Assert.Equal(driver, trip.OfferedDriverId);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Confirm_khong_con_tai_xe_thi_Failed()
    {
        var rider = Guid.NewGuid();
        var trip = Stored(TestTrips.Make(rider));
        LoginAs(rider); MatchReturns(null);

        var dto = await new ConfirmTripHandler(_repo.Object, _uow.Object, _user.Object, Dispatcher())
            .HandleAsync(new ConfirmTripCommand(trip.Id));

        Assert.Equal("Failed", dto.Status);
    }

    [Fact]
    public async Task Confirm_khong_phai_chu_chuyen_nem_Forbidden_va_khong_goi_Matching()
    {
        var trip = Stored(TestTrips.Make());
        LoginAs(Guid.NewGuid());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => new ConfirmTripHandler(_repo.Object, _uow.Object, _user.Object, Dispatcher())
                .HandleAsync(new ConfirmTripCommand(trip.Id)));

        _matching.Verify(m => m.FindNearestDriverAsync(It.IsAny<MatchRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Confirm_Matching_loi_thi_khong_luu_gi()
    {
        var rider = Guid.NewGuid();
        var trip = Stored(TestTrips.Make(rider));
        LoginAs(rider);
        _matching.Setup(m => m.FindNearestDriverAsync(It.IsAny<MatchRequest>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new PricingUnavailableException("down"));

        await Assert.ThrowsAsync<PricingUnavailableException>(
            () => new ConfirmTripHandler(_repo.Object, _uow.Object, _user.Object, Dispatcher())
                .HandleAsync(new ConfirmTripCommand(trip.Id)));

        SaveNever();
    }

    // ---------- Decline ----------

    [Fact]
    public async Task Decline_loai_tai_xe_vua_tu_choi_va_de_xuat_cho_nguoi_ke()
    {
        var first = Guid.NewGuid(); var second = Guid.NewGuid();
        var trip = Stored(TestTrips.Offered(first));
        LoginAs(first); MatchReturns(second);

        var dto = await new DeclineTripHandler(_repo.Object, _uow.Object, _user.Object, Dispatcher())
            .HandleAsync(new DeclineTripCommand(trip.Id));

        Assert.Equal("DriverOffered", dto.Status);
        Assert.Equal(second, trip.OfferedDriverId);
        _matching.Verify(m => m.FindNearestDriverAsync(
            It.Is<MatchRequest>(r => r.ExcludeDriverIds.Contains(first)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Decline_sai_tai_xe_nem_Forbidden()
    {
        var trip = Stored(TestTrips.Offered(Guid.NewGuid()));
        LoginAs(Guid.NewGuid());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => new DeclineTripHandler(_repo.Object, _uow.Object, _user.Object, Dispatcher())
                .HandleAsync(new DeclineTripCommand(trip.Id)));

        SaveNever();
    }

    // ---------- Accept ----------

    [Fact]
    public async Task Accept_dung_tai_xe_duoc_de_xuat()
    {
        var driver = Guid.NewGuid();
        var trip = Stored(TestTrips.Offered(driver));
        LoginAs(driver);

        var dto = await new AcceptTripHandler(_repo.Object, _uow.Object, _user.Object)
            .HandleAsync(new AcceptTripCommand(trip.Id));

        Assert.Equal("DriverAccepted", dto.Status);
        Assert.Equal(driver, dto.DriverId);
    }

    [Fact]
    public async Task Accept_tai_xe_khong_duoc_de_xuat_nem_Forbidden()
    {
        var trip = Stored(TestTrips.Offered(Guid.NewGuid()));
        LoginAs(Guid.NewGuid());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => new AcceptTripHandler(_repo.Object, _uow.Object, _user.Object)
                .HandleAsync(new AcceptTripCommand(trip.Id)));

        SaveNever();
    }

    // ---------- Cancel ----------

    [Fact]
    public async Task Cancel_boi_chu_chuyen_khi_con_o_buoc_bao_gia()
    {
        var rider = Guid.NewGuid();
        var trip = Stored(TestTrips.Make(rider));
        LoginAs(rider);

        var dto = await new CancelTripHandler(_repo.Object, _uow.Object, _user.Object)
            .HandleAsync(new CancelTripCommand(trip.Id));

        Assert.Equal("Cancelled", dto.Status);
    }

    [Fact]
    public async Task Cancel_boi_nguoi_khac_nem_Forbidden()
    {
        var trip = Stored(TestTrips.Make());
        LoginAs(Guid.NewGuid());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => new CancelTripHandler(_repo.Object, _uow.Object, _user.Object)
                .HandleAsync(new CancelTripCommand(trip.Id)));
    }
}