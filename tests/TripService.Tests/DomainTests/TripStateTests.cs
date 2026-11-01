using TripService.Domain.Common;
using TripService.Domain.Trips;
using Xunit;

namespace TripService.Tests.DomainTests;

public class TripStateTests
{
    public enum Act { Confirm, OfferTo, NoDriverFound, Accept, Decline, PickUp, DropOff, MarkPaid, MarkPaymentPending, Cancel }

    // Bảng chuyển trạng thái hợp lệ. Mọi ô không có trong bảng này phải bị từ chối
    private static readonly Dictionary<(TripStatus From, Act Action), TripStatus> Valid = new()
    {
        [(TripStatus.Requested, Act.Confirm)] = TripStatus.Searching,
        [(TripStatus.Requested, Act.Cancel)] = TripStatus.Cancelled,

        [(TripStatus.Searching, Act.OfferTo)] = TripStatus.DriverOffered,
        [(TripStatus.Searching, Act.NoDriverFound)] = TripStatus.Failed,

        [(TripStatus.DriverOffered, Act.Accept)] = TripStatus.DriverAccepted,
        [(TripStatus.DriverOffered, Act.Decline)] = TripStatus.Searching,   // lần từ chối đầu (<= k)
        [(TripStatus.DriverOffered, Act.Cancel)] = TripStatus.Cancelled,

        [(TripStatus.DriverAccepted, Act.PickUp)] = TripStatus.PickedUp,
        [(TripStatus.DriverAccepted, Act.Cancel)] = TripStatus.Cancelled,

        [(TripStatus.PickedUp, Act.DropOff)] = TripStatus.DroppedOff,

        [(TripStatus.DroppedOff, Act.MarkPaid)] = TripStatus.Paid,
        [(TripStatus.DroppedOff, Act.MarkPaymentPending)] = TripStatus.PaymentPending,
        [(TripStatus.PaymentPending, Act.MarkPaid)] = TripStatus.Paid,
        [(TripStatus.PaymentPending, Act.MarkPaymentPending)] = TripStatus.PaymentPending,   // lặp lại: giữ nguyên
        [(TripStatus.Paid, Act.MarkPaid)] = TripStatus.Paid,                                 // lặp lại: giữ nguyên
        [(TripStatus.Paid, Act.MarkPaymentPending)] = TripStatus.Paid,                       // đến muộn sau khi Paid: bỏ qua
    };

    public static IEnumerable<object[]> ValidCases()
        => Valid.Select(kv => new object[] { kv.Key.From, kv.Key.Action, kv.Value });

    public static IEnumerable<object[]> InvalidCases()
        => from s in Enum.GetValues<TripStatus>()
           from a in Enum.GetValues<Act>()
           where !Valid.ContainsKey((s, a))
           select new object[] { s, a };

    // Đưa chuyến tới trạng thái cần test bằng đường đi hợp lệ
    private static Trip TripAt(TripStatus target)
    {
        var t = TestTrips.Make();
        if (target == TripStatus.Requested) return t;
        if (target == TripStatus.Cancelled) { t.Cancel(); return t; }

        t.Confirm();
        if (target == TripStatus.Searching) return t;
        if (target == TripStatus.Failed) { t.NoDriverFound(); return t; }

        t.OfferTo(Guid.NewGuid());
        if (target == TripStatus.DriverOffered) return t;

        t.AcceptBy(t.OfferedDriverId!.Value);
        if (target == TripStatus.DriverAccepted) return t;
        t.PickUp();
        if (target == TripStatus.PickedUp) return t;
        t.DropOff();
        if (target == TripStatus.DroppedOff) return t;
        if (target == TripStatus.PaymentPending) { t.MarkPaymentPending(); return t; }
        t.MarkPaid();   // Paid
        return t;
    }

    private static void Do(Trip t, Act action)
    {
        switch (action)
        {
            case Act.Confirm: t.Confirm(); break;
            case Act.OfferTo: t.OfferTo(Guid.NewGuid()); break;
            case Act.NoDriverFound: t.NoDriverFound(); break;
            case Act.Accept: t.AcceptBy(t.OfferedDriverId ?? Guid.NewGuid()); break;
            case Act.Decline: t.DeclineBy(t.OfferedDriverId ?? Guid.NewGuid()); break;
            case Act.PickUp: t.PickUp(); break;
            case Act.DropOff: t.DropOff(); break;
            case Act.MarkPaid: t.MarkPaid(); break;
            case Act.MarkPaymentPending: t.MarkPaymentPending(); break;
            case Act.Cancel: t.Cancel(); break;
        }
    }

    [Theory]
    [MemberData(nameof(ValidCases))]
    public void Chuyen_trang_thai_hop_le(TripStatus from, Act action, TripStatus to)
    {
        var trip = TripAt(from);
        Do(trip, action);
        Assert.Equal(to, trip.Status);
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void Chuyen_trang_thai_sai_nem_DomainException_va_khong_doi_gi(TripStatus from, Act action)
    {
        var trip = TripAt(from);
        Assert.Throws<DomainException>(() => Do(trip, action));
        Assert.Equal(from, trip.Status);
    }

    [Fact]
    public void Chi_tai_xe_duoc_de_xuat_moi_nhan_duoc_chuyen()
    {
        var offered = Guid.NewGuid();
        var trip = TestTrips.Offered(offered);

        Assert.Throws<DomainException>(() => trip.AcceptBy(Guid.NewGuid()));

        Assert.Equal(TripStatus.DriverOffered, trip.Status);
        Assert.Null(trip.DriverId);
        Assert.Equal(offered, trip.OfferedDriverId);
    }

    [Fact]
    public void Nhan_chuyen_xoa_loi_de_xuat_va_gan_tai_xe()
    {
        var driver = Guid.NewGuid();
        var trip = TestTrips.Accepted(driver);

        Assert.Equal(driver, trip.DriverId);
        Assert.Null(trip.OfferedDriverId);
    }

    [Fact]
    public void Tai_xe_khac_khong_the_tu_choi_thay()
    {
        var trip = TestTrips.Offered(Guid.NewGuid());
        Assert.Throws<DomainException>(() => trip.DeclineBy(Guid.NewGuid()));
        Assert.Equal(0, trip.DeclineCount);
    }

    [Fact]
    public void Tai_xe_da_tu_choi_khong_the_duoc_de_xuat_lai()
    {
        var driver = Guid.NewGuid();
        var trip = TestTrips.Offered(driver);
        trip.DeclineBy(driver);

        Assert.Equal(TripStatus.Searching, trip.Status);
        Assert.Contains(driver, trip.DeclinedDriverIds);
        Assert.Throws<DomainException>(() => trip.OfferTo(driver));
    }

    [Fact]
    public void Tu_choi_k_lan_van_tim_tiep_lan_k_cong_1_thi_Failed()
    {
        var trip = TestTrips.Make();
        trip.Confirm();

        for (var i = 1; i <= Trip.MaxDeclines; i++)
        {
            trip.OfferTo(Guid.NewGuid());
            trip.DeclineBy(trip.OfferedDriverId!.Value);
            Assert.Equal(TripStatus.Searching, trip.Status);   // <= k: quay lại Matching
        }

        trip.OfferTo(Guid.NewGuid());
        trip.DeclineBy(trip.OfferedDriverId!.Value);

        Assert.Equal(TripStatus.Failed, trip.Status);          // > k: không thành công
        Assert.Equal(Trip.MaxDeclines + 1, trip.DeclineCount);
    }
}