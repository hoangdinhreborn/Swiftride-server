using BuildingBlocks.Security;
using Moq;
using TripService.Application.Common.Exceptions;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Commands.DropOffTrip;
using TripService.Application.Trips.Events;
using TripService.Application.Trips.Interfaces;
using TripService.Domain.Trips;
using Xunit;

namespace TripService.Tests.ApplicationTests;

public class DropOffEventTests
{
    private readonly Mock<ITripRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IOutbox> _outbox = new();

    private (Trip trip, Guid driverId) PickedUpTrip()
    {
        var driverId = Guid.NewGuid();
        var trip = TestTrips.Accepted(driverId, fare: 85_000m);
        trip.PickUp();
        _repo.Setup(r => r.GetByIdAsync(trip.Id, It.IsAny<CancellationToken>())).ReturnsAsync(trip);
        return (trip, driverId);
    }

    private DropOffTripHandler Handler() => new(_repo.Object, _uow.Object, _user.Object, _outbox.Object);

    [Fact]
    public async Task DropOff_dua_RideCompleted_vao_outbox_voi_dung_so_tien()
    {
        var (trip, driverId) = PickedUpTrip();
        _user.Setup(u => u.Id).Returns(driverId);

        await Handler().HandleAsync(new DropOffTripCommand(trip.Id));

        _outbox.Verify(o => o.Enqueue(RideEvents.Completed,
            It.Is<RideCompleted>(e => e.TripId == trip.Id && e.RiderId == trip.RiderId
                                      && e.DriverId == driverId && e.Amount == 85_000m)), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DropOff_sai_tai_xe_khong_ghi_outbox_va_khong_luu()
    {
        var (trip, _) = PickedUpTrip();
        _user.Setup(u => u.Id).Returns(Guid.NewGuid());

        await Assert.ThrowsAsync<ForbiddenException>(() => Handler().HandleAsync(new DropOffTripCommand(trip.Id)));

        _outbox.Verify(o => o.Enqueue(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}