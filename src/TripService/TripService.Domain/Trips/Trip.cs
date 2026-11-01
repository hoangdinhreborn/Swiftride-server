using TripService.Domain.Common;
using TripService.Domain.Trips.States;

namespace TripService.Domain.Trips;

public class Trip
{
    public const int MaxLocationLength = 300;
    public const int MaxDeclines = 3;   // k trong flowchart

    private List<Guid> _declined = [];

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RiderId { get; private set; }
    public Guid? DriverId { get; private set; }           // tài xế đã nhận chuyến
    public Guid? OfferedDriverId { get; private set; }    // tài xế đang được đề xuất, chờ trả lời
    public string Pickup { get; private set; } = "";
    public double PickupLat { get; private set; }
    public double PickupLng { get; private set; }
    public string Destination { get; private set; } = "";
    public double DestinationLat { get; private set; }
    public double DestinationLng { get; private set; }
    public decimal QuotedFare { get; private set; }
    public TripStatus Status { get; private set; } = TripStatus.Requested;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public IReadOnlyList<Guid> DeclinedDriverIds => _declined;
    public int DeclineCount => _declined.Count;

    private TripState State => TripState.For(Status);

    private Trip() { }

    public static Trip Request(
        Guid riderId,
        string pickup, double pickupLat, double pickupLng,
        string destination, double destinationLat, double destinationLng,
        decimal quotedFare)
    {
        if (riderId == Guid.Empty)
            throw new DomainValidationException("Thiếu thông tin người đặt xe");
        if (quotedFare <= 0)
            throw new DomainValidationException("Giá cước phải lớn hơn 0");

        return new()
        {
            RiderId = riderId,
            Pickup = Normalize(pickup, "Điểm đón"),
            PickupLat = Latitude(pickupLat, "điểm đón"),
            PickupLng = Longitude(pickupLng, "điểm đón"),
            Destination = Normalize(destination, "Điểm đến"),
            DestinationLat = Latitude(destinationLat, "điểm đến"),
            DestinationLng = Longitude(destinationLng, "điểm đến"),
            QuotedFare = quotedFare
        };
    }

    // Mỗi hành động hỏi state hiện tại trước. State từ chối thì ném DomainException và Trip không bị đổi gì
    public void Confirm() => MoveTo(State.Confirm());
    public void NoDriverFound() => MoveTo(State.NoDriverFound());
    public void Cancel() => MoveTo(State.Cancel());
    public void PickUp() => MoveTo(State.PickUp());
    public void DropOff() => MoveTo(State.DropOff());

    // Idempotent: broker giao at-least-once, cùng một kết quả có thể đến nhiều lần
    public void MarkPaid() => MoveTo(State.MarkPaid());
    public void MarkPaymentPending() => MoveTo(State.MarkPaymentPending());

    public void OfferTo(Guid driverId)
    {
        var next = State.OfferTo();
        if (driverId == Guid.Empty) throw new DomainValidationException("Thiếu thông tin tài xế");
        if (_declined.Contains(driverId)) throw new DomainException("Tài xế này đã từ chối chuyến");

        MoveTo(next);
        OfferedDriverId = driverId;
    }

    public void AcceptBy(Guid driverId)
    {
        var next = State.Accept();
        if (OfferedDriverId != driverId) throw new DomainException("Chuyến này đang được đề xuất cho tài xế khác");

        DriverId = driverId;
        MoveTo(next);
    }

    public void DeclineBy(Guid driverId)
    {
        var next = State.Decline(_declined.Count + 1);
        if (OfferedDriverId != driverId) throw new DomainException("Chuyến này đang được đề xuất cho tài xế khác");

        _declined.Add(driverId);
        MoveTo(next);
    }

    private void MoveTo(TripState next)
    {
        Status = next.Status;
        if (Status != TripStatus.DriverOffered) OfferedDriverId = null;   // hết lời đề xuất
    }

    private static string Normalize(string? value, string name)
    {
        var v = value?.Trim();
        if (string.IsNullOrEmpty(v))
            throw new DomainValidationException($"{name} không được để trống");
        if (v.Length > MaxLocationLength)
            throw new DomainValidationException($"{name} tối đa {MaxLocationLength} ký tự");
        return v;
    }

    // Viết kiểu v >= x && v <= y để NaN cũng bị từ chối
    private static double Latitude(double v, string name)
        => v >= -90 && v <= 90 ? v : throw new DomainValidationException($"Vĩ độ {name} phải nằm trong khoảng -90 đến 90");

    private static double Longitude(double v, string name)
        => v >= -180 && v <= 180 ? v : throw new DomainValidationException($"Kinh độ {name} phải nằm trong khoảng -180 đến 180");
}