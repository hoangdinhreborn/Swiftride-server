using Moq;
using TripService.Application.Common.Exceptions;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Commands.ApplyPaymentResult;
using TripService.Application.Trips.Interfaces;
using TripService.Domain.Trips;
using Xunit;

namespace TripService.Tests.ApplicationTests;

public class ApplyPaymentResultTests
{
    private readonly Mock<ITripRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private Trip DroppedOffTrip()
    {
        var trip = TestTrips.Make();
        trip.AcceptBy(Guid.NewGuid());
        trip.PickUp();
        trip.DropOff();
        _repo.Setup(r => r.GetByIdAsync(trip.Id, It.IsAny<CancellationToken>())).ReturnsAsync(trip);
        return trip;
    }

    private ApplyPaymentResultHandler Handler() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Thanh_toan_thanh_cong_chuyen_sang_Paid_va_luu()
    {
        var trip = DroppedOffTrip();
        var dto = await Handler().HandleAsync(new ApplyPaymentResultCommand(trip.Id, true));

        Assert.Equal("Paid", dto.Status);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Thanh_toan_that_bai_chuyen_sang_PaymentPending()
    {
        var trip = DroppedOffTrip();
        var dto = await Handler().HandleAsync(new ApplyPaymentResultCommand(trip.Id, false));

        Assert.Equal("PaymentPending", dto.Status);
    }

    [Fact]
    public async Task Chuyen_khong_ton_tai_nem_NotFoundException()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Trip?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => Handler().HandleAsync(new ApplyPaymentResultCommand(Guid.NewGuid(), true)));
    }
}