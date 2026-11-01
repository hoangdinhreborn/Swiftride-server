namespace TripService.Domain.Trips.States;

internal sealed class DriverOfferedState : TripState
{
    public static readonly DriverOfferedState Instance = new();
    private DriverOfferedState() { }

    public override TripStatus Status => TripStatus.DriverOffered;
    public override TripState Accept() => DriverAcceptedState.Instance;
    public override TripState Cancel() => CancelledState.Instance;

    // Từ chối lần thứ n: n <= k thì quay lại tìm tài xế khác, n > k thì kết thúc không thành công
    public override TripState Decline(int declineCount)
        => declineCount > Trip.MaxDeclines ? FailedState.Instance : SearchingState.Instance;
}
