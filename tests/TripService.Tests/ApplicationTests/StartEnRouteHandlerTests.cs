using BuildingBlocks.Security;
using Moq;
using TripService.Application.Common.Exceptions;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Commands.StartEnRoute;
using TripService.Application.Trips.Interfaces;
using TripService.Domain.Trips;
using Xunit;

namespace TripService.Tests.ApplicationTests;

public class StartEnRouteHandlerTests
{
    private readonly Mock<ITripRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();

    private StartEnRouteHandler Handler() => new(_repo.Object, _uow.Object, _user.Object);

    private Trip Stored(Trip t)
    {
        _repo.Setup(r => r.GetByIdAsync(t.Id, It.IsAny<CancellationToken>())).ReturnsAsync(t);
        return t;
    }

    [Fact]
    public async Task Dung_tai_xe_chuyen_sang_EnRoute_va_luu()
    {
        var driver = Guid.NewGuid();
        var trip = Stored(TestTrips.Accepted(driver));
        _user.Setup(u => u.Id).Returns(driver);

        var dto = await Handler().HandleAsync(new StartEnRouteCommand(trip.Id));

        Assert.Equal("EnRoute", dto.Status);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sai_tai_xe_nem_Forbidden_va_khong_luu()
    {
        var trip = Stored(TestTrips.Accepted());
        _user.Setup(u => u.Id).Returns(Guid.NewGuid());

        await Assert.ThrowsAsync<ForbiddenException>(() => Handler().HandleAsync(new StartEnRouteCommand(trip.Id)));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}