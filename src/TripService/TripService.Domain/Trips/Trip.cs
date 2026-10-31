using TripService.Domain.Common;

namespace TripService.Domain.Trips;

public class Trip
{
    public const int MaxLocationLength = 300;

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RiderId { get; private set; }
    public Guid? DriverId { get; private set; }
    public string Pickup { get; private set; } = "";
    public double PickupLat { get; private set; }
    public double PickupLng { get; private set; }
    public string Destination { get; private set; } = "";
    public double DestinationLat { get; private set; }
    public double DestinationLng { get; private set; }
    public decimal QuotedFare { get; private set; }
    public TripStatus Status { get; private set; } = TripStatus.Requested;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

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

    public void AcceptBy(Guid driverId) { Require(TripStatus.Requested); DriverId = driverId; Status = TripStatus.DriverAccepted; }
    public void PickUp() { Require(TripStatus.DriverAccepted); Status = TripStatus.PickedUp; }
    public void DropOff() { Require(TripStatus.PickedUp); Status = TripStatus.DroppedOff; }
    // Idempotent: broker giao at-least-once nên cùng kết quả có thể đến nhiều lần
    public void MarkPaid()
    {
        if (Status == TripStatus.Paid) return;
        if (Status is not (TripStatus.DroppedOff or TripStatus.PaymentPending))
            throw new DomainException($"Không thể ghi nhận thanh toán khi chuyến đang ở trạng thái {Status}");
        Status = TripStatus.Paid;
    }

    // Thanh toán thất bại đến muộn sau khi đã Paid (sai thứ tự) thì bỏ qua
    public void MarkPaymentPending()
    {
        if (Status is TripStatus.PaymentPending or TripStatus.Paid) return;
        Require(TripStatus.DroppedOff);
        Status = TripStatus.PaymentPending;
    }

    private void Require(TripStatus expected)
    {
        if (Status != expected)
            throw new DomainException($"Trạng thái hiện tại là {Status}, cần {expected}");
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

    // Viết kiểu a >= x && a <= y để NaN cũng bị từ chối
    private static double Latitude(double v, string name)
        => v >= -90 && v <= 90 ? v : throw new DomainValidationException($"Vĩ độ {name} phải nằm trong khoảng -90 đến 90");

    private static double Longitude(double v, string name)
        => v >= -180 && v <= 180 ? v : throw new DomainValidationException($"Kinh độ {name} phải nằm trong khoảng -180 đến 180");
}