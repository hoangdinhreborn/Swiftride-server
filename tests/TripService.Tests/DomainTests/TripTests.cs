using TripService.Domain.Common;
using TripService.Domain.Trips;
using Xunit;

namespace TripService.Tests.DomainTests;

public class TripTests
{
    private static Trip NewTrip() => TestTrips.Make();

    [Fact]
    public void Request_tao_chuyen_o_trang_thai_Requested()
    {
        var riderId = Guid.NewGuid();
        var trip = TestTrips.Make(riderId);

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
        () => TestTrips.Make(pickup: pickup!, destination: dest));

    [Fact]
    public void Request_dia_diem_qua_dai_nem_DomainValidationException()
        => Assert.Throws<DomainValidationException>(
            () => TestTrips.Make(pickup: new string('a', 301)));

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

    private static Trip DroppedOffTrip()
    {
        var trip = TestTrips.Make();
        trip.AcceptBy(Guid.NewGuid());
        trip.PickUp();
        trip.DropOff();
        return trip;
    }

    [Fact]
    public void MarkPaid_tu_DroppedOff_thanh_Paid()
    {
        var trip = DroppedOffTrip();
        trip.MarkPaid();
        Assert.Equal(TripStatus.Paid, trip.Status);
    }

    [Fact]
    public void MarkPaid_hai_lan_khong_loi()
    {
        var trip = DroppedOffTrip();
        trip.MarkPaid();
        trip.MarkPaid();
        Assert.Equal(TripStatus.Paid, trip.Status);
    }

    [Fact]
    public void MarkPaid_khi_chua_tra_khach_nem_loi()
        => Assert.Throws<DomainException>(() => TestTrips.Make().MarkPaid());

    [Fact]
    public void MarkPaymentPending_tu_DroppedOff_roi_van_Paid_duoc()
    {
        var trip = DroppedOffTrip();
        trip.MarkPaymentPending();
        Assert.Equal(TripStatus.PaymentPending, trip.Status);

        trip.MarkPaid();   // rider thử lại và thành công
        Assert.Equal(TripStatus.Paid, trip.Status);
    }

    [Fact]
    public void Ket_qua_that_bai_den_muon_sau_khi_Paid_bi_bo_qua()
    {
        var trip = DroppedOffTrip();
        trip.MarkPaid();
        trip.MarkPaymentPending();
        Assert.Equal(TripStatus.Paid, trip.Status);
    }
}