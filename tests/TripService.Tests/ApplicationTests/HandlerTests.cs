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

    private void LoginAs(Guid id) => _user.Setup(u => u.Id).Returns(id);

    private Trip AcceptedTripBy(Guid driverId)
    {
        var trip = Trip.Request(Guid.NewGuid(), "Q1", "Q7");
        trip.AcceptBy(driverId);
        _repo.Setup(r => r.GetByIdAsync(trip.Id, It.IsAny<CancellationToken>())).ReturnsAsync(trip);
        return trip;
    }

    // ---------- RequestTrip ----------

    [Fact]
    public async Task RequestTrip_hop_le_luu_chuyen_va_lay_RiderId_tu_token()
    {
        var riderId = Guid.NewGuid();
        LoginAs(riderId);
        var handler = new RequestTripHandler(_repo.Object, _uow.Object, _user.Object);

        var dto = await handler.HandleAsync(new RequestTripCommand("Q1", "Q7"));

        Assert.Equal(riderId, dto.RiderId);
        Assert.Equal("Requested", dto.Status);
        _repo.Verify(r => r.AddAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("", "Q7")]
    [InlineData("Q1", "  ")]
    public async Task RequestTrip_dia_diem_rong_nem_ValidationException_va_khong_luu(string pickup, string dest)
    {
        LoginAs(Guid.NewGuid());
        var handler = new RequestTripHandler(_repo.Object, _uow.Object, _user.Object);

        await Assert.ThrowsAsync<DomainValidationException>(
    () => handler.HandleAsync(new RequestTripCommand(pickup, dest)));

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
        LoginAs(Guid.NewGuid());   // tài xế khác
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