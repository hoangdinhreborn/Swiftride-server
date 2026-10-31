using TripService.Domain.Common;
using TripService.Domain.Trips;
using Xunit;

namespace TripService.Tests.DomainTests;

public class TripTests
{
    private static Trip NewTrip() => Trip.Request(Guid.NewGuid(), "Q1", "Q7");

    [Fact]
    public void Request_tao_chuyen_o_trang_thai_Requested()
    {
        var riderId = Guid.NewGuid();
        var trip = Trip.Request(riderId, "Q1", "Q7");

        Assert.Equal(TripStatus.Requested, trip.Status);
        Assert.Equal(riderId, trip.RiderId);
        Assert.Null(trip.DriverId);
    }

    [Fact]
    public void AcceptBy_gan_tai_xe_va_chuyen_sang_DriverAccepted()
    {
        var trip = NewTrip();
        var driverId = Guid.NewGuid();

        trip.AcceptBy(driverId);

        Assert.Equal(TripStatus.DriverAccepted, trip.Status);
        Assert.Equal(driverId, trip.DriverId);
    }

    [Fact]
    public void AcceptBy_hai_lan_nem_loi()
    {
        var trip = NewTrip();
        trip.AcceptBy(Guid.NewGuid());

        Assert.Throws<DomainException>(() => trip.AcceptBy(Guid.NewGuid()));
    }

    [Fact]
    public void PickUp_khi_chua_co_tai_xe_nem_loi()
        => Assert.Throws<DomainException>(() => NewTrip().PickUp());

    [Fact]
    public void DropOff_khi_chua_don_khach_nem_loi()
    {
        var trip = NewTrip();
        trip.AcceptBy(Guid.NewGuid());

        Assert.Throws<DomainException>(() => trip.DropOff());
    }

    [Fact]
    public void Di_het_luong_den_DroppedOff()
    {
        var trip = NewTrip();
        trip.AcceptBy(Guid.NewGuid());
        trip.PickUp();
        trip.DropOff();

        Assert.Equal(TripStatus.DroppedOff, trip.Status);
    }

        [Theory]
    [InlineData("", "Q7")]
    [InlineData("Q1", "  ")]
    [InlineData(null, "Q7")]
    public void Request_dia_diem_rong_nem_DomainValidationException(string? pickup, string dest)
        => Assert.Throws<DomainValidationException>(
            () => Trip.Request(Guid.NewGuid(), pickup!, dest));

    [Fact]
    public void Request_dia_diem_qua_dai_nem_DomainValidationException()
        => Assert.Throws<DomainValidationException>(
            () => Trip.Request(Guid.NewGuid(), new string('a', 301), "Q7"));

    [Fact]
    public void Request_cat_khoang_trang_hai_dau()
    {
        var trip = Trip.Request(Guid.NewGuid(), "  Q1 ", " Q7  ");
        Assert.Equal("Q1", trip.Pickup);
        Assert.Equal("Q7", trip.Destination);
    }
}