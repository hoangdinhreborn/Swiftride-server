using BuildingBlocks.Security;
using Moq;
using PaymentService.Application.Common.Exceptions;
using PaymentService.Application.Payments.Interfaces;
using PaymentService.Application.Payments.Queries.GetPaymentByTrip;
using PaymentService.Domain.Payments;
using Xunit;

namespace PaymentService.Tests;

public class GetPaymentByTripHandlerTests
{
    private readonly Mock<IPaymentRepository> _repo = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Guid _trip = Guid.NewGuid();
    private readonly Guid _payer = Guid.NewGuid();

    private GetPaymentByTripHandler Handler() => new(_repo.Object, _user.Object);

    private Payment Stored()
    {
        var p = Payment.Initiate(_trip, _payer, 76_000m, "k");
        _repo.Setup(r => r.GetLatestByTripAsync(_trip, It.IsAny<CancellationToken>())).ReturnsAsync(p);
        return p;
    }

    [Fact]
    public async Task Nguoi_tra_tien_xem_duoc()
    {
        var p = Stored();
        _user.Setup(u => u.Id).Returns(_payer);

        var dto = await Handler().HandleAsync(new GetPaymentByTripQuery(_trip));

        Assert.Equal(p.Id, dto.Id);
    }

    [Fact]
    public async Task Admin_xem_duoc()
    {
        Stored();
        _user.Setup(u => u.Id).Returns(Guid.NewGuid());
        _user.Setup(u => u.Role).Returns(Roles.Admin);

        await Handler().HandleAsync(new GetPaymentByTripQuery(_trip));
    }

    [Fact]
    public async Task Nguoi_khac_bi_Forbidden()
    {
        Stored();
        _user.Setup(u => u.Id).Returns(Guid.NewGuid());
        _user.Setup(u => u.Role).Returns(Roles.Driver);

        await Assert.ThrowsAsync<ForbiddenException>(() => Handler().HandleAsync(new GetPaymentByTripQuery(_trip)));
    }

    [Fact]
    public async Task Chua_co_thanh_toan_thi_NotFound()
    {
        _repo.Setup(r => r.GetLatestByTripAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Payment?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Handler().HandleAsync(new GetPaymentByTripQuery(_trip)));
    }
}