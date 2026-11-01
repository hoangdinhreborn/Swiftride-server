using TripService.Domain.Common;
using TripService.Domain.Trips;
using Xunit;

namespace TripService.Tests.DomainTests;

public class TripTests
{
    [Fact]
    public void Request_tao_chuyen_o_trang_thai_Requested()
    {
        var riderId = Guid.NewGuid();
        var trip = TestTrips.Make(riderId);

        Assert.Equal(TripStatus.Requested, trip.Status);
        Assert.Equal(riderId, trip.RiderId);
        Assert.Null(trip.DriverId);
        Assert.Null(trip.OfferedDriverId);
        Assert.Equal(0, trip.DeclineCount);
    }

    [Theory]
    [InlineData("", "Q7")]
    [InlineData("Q1", "  ")]
    [InlineData(null, "Q7")]
    public void Request_dia_diem_rong_nem_DomainValidationException(string? pickup, string dest)
        => Assert.Throws<DomainValidationException>(() => TestTrips.Make(pickup: pickup!, destination: dest));

    [Fact]
    public void Request_dia_diem_qua_dai_nem_DomainValidationException()
        => Assert.Throws<DomainValidationException>(() => TestTrips.Make(pickup: new string('a', 301)));

    [Fact]
    public void Request_cat_khoang_trang_hai_dau()
    {
        var trip = TestTrips.Make(pickup: "  Q1 ", destination: " Q7  ");
        Assert.Equal("Q1", trip.Pickup);
        Assert.Equal("Q7", trip.Destination);
    }

    [Theory]
    [InlineData(91, 106)]
    [InlineData(10, 181)]
    [InlineData(double.NaN, 106)]
    public void Request_toa_do_sai_nem_DomainValidationException(double lat, double lng)
        => Assert.Throws<DomainValidationException>(() => TestTrips.Make(pickupLat: lat, pickupLng: lng));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Request_gia_khong_duong_nem_DomainValidationException(decimal fare)
        => Assert.Throws<DomainValidationException>(() => TestTrips.Make(fare: fare));

    [Fact]
    public void Request_luu_gia_da_bao()
        => Assert.Equal(85_000m, TestTrips.Make(fare: 85_000m).QuotedFare);

    [Fact]
    public void Di_het_luong_thanh_cong_den_Paid()
    {
        var trip = TestTrips.Accepted();
        trip.PickUp();
        trip.DropOff();
        trip.MarkPaid();
        Assert.Equal(TripStatus.Paid, trip.Status);
    }
}