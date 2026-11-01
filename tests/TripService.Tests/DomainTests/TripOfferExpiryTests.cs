using TripService.Domain.Trips;
using Xunit;

namespace TripService.Tests.DomainTests;

public class TripOfferExpiryTests
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

    [Fact]
    public void Chua_de_xuat_thi_chua_co_OfferedAt()
        => Assert.Null(TestTrips.Make().OfferedAt);

    [Fact]
    public void De_xuat_ghi_lai_thoi_diem()
    {
        var before = DateTime.UtcNow;
        var trip = TestTrips.Offered(Guid.NewGuid());
        Assert.InRange(trip.OfferedAt!.Value, before, DateTime.UtcNow);
    }

    [Fact]
    public void Chua_het_han_trong_thoi_gian_cho()
        => Assert.False(TestTrips.Offered(Guid.NewGuid()).IsOfferExpired(DateTime.UtcNow.AddSeconds(5), Ttl));

    [Fact]
    public void Het_han_sau_ttl()
        => Assert.True(TestTrips.Offered(Guid.NewGuid()).IsOfferExpired(DateTime.UtcNow.AddSeconds(31), Ttl));

    [Fact]
    public void Chuyen_khong_o_DriverOffered_khong_bao_gio_het_han()
    {
        var far = DateTime.UtcNow.AddDays(1);
        Assert.False(TestTrips.Make().IsOfferExpired(far, Ttl));        // Requested
        Assert.False(TestTrips.Accepted().IsOfferExpired(far, Ttl));    // DriverAccepted
    }

    [Fact]
    public void Tai_xe_nhan_chuyen_thi_xoa_OfferedAt()
    {
        var d = Guid.NewGuid();
        var trip = TestTrips.Offered(d);
        trip.AcceptBy(d);
        Assert.Null(trip.OfferedAt);
    }

    [Fact]
    public void Tu_choi_thi_xoa_OfferedAt_cho_den_khi_de_xuat_nguoi_ke()
    {
        var d = Guid.NewGuid();
        var trip = TestTrips.Offered(d);
        trip.DeclineBy(d);
        Assert.Null(trip.OfferedAt);

        trip.OfferTo(Guid.NewGuid());
        Assert.NotNull(trip.OfferedAt);
    }
}