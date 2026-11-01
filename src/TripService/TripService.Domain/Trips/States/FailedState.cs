namespace TripService.Domain.Trips.States;

// Trạng thái kết thúc: mọi hành động đều bị từ chối theo mặc định
internal sealed class FailedState : TripState
{
    public static readonly FailedState Instance = new();
    private FailedState() { }

    public override TripStatus Status => TripStatus.Failed;
}
