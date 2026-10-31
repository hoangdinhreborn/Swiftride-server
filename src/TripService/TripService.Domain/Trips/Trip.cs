using TripService.Domain.Common;

namespace TripService.Domain.Trips;

public class Trip
{
    public const int MaxLocationLength = 300;
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RiderId { get; private set; }
    public Guid? DriverId { get; private set; }
    public string Pickup { get; private set; } = "";
    public string Destination { get; private set; } = "";
    public TripStatus Status { get; private set; } = TripStatus.Requested;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private Trip() { }

    public static Trip Request(Guid riderId, string pickup, string destination)
    {
        if (riderId == Guid.Empty)
            throw new DomainValidationException("Thiếu thông tin người đặt xe");

        return new()
        {
            RiderId = riderId,
            Pickup = Normalize(pickup, "Điểm đón"),
            Destination = Normalize(destination, "Điểm đến")
        };
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

    public void AcceptBy(Guid driverId) { Require(TripStatus.Requested); DriverId = driverId; Status = TripStatus.DriverAccepted; }
    public void PickUp() { Require(TripStatus.DriverAccepted); Status = TripStatus.PickedUp; }
    public void DropOff() { Require(TripStatus.PickedUp); Status = TripStatus.DroppedOff; }

    private void Require(TripStatus expected)
    {
        if (Status != expected)
            throw new DomainException($"Trạng thái hiện tại là {Status}, cần {expected}");
    }
}