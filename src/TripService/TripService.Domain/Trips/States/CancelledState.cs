namespace TripService.Domain.Trips.States;

// Trạng thái kết thúc: mọi hành động đều bị từ chối theo mặc định
internal sealed class CancelledState : TripState
{
    public static readonly CancelledState Instance = new();
    private CancelledState() { }

    public override TripStatus Status => TripStatus.Cancelled;
}
