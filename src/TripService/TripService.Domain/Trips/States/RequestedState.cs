namespace TripService.Domain.Trips.States;

internal sealed class RequestedState : TripState
{
    public static readonly RequestedState Instance = new();
    private RequestedState() { }

    public override TripStatus Status => TripStatus.Requested;
    public override TripState Confirm() => SearchingState.Instance;   // rider đồng ý giá
    public override TripState Cancel() => CancelledState.Instance;    // rider từ chối giá
}
