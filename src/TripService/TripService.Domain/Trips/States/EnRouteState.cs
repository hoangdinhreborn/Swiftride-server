namespace TripService.Domain.Trips.States;

internal sealed class EnRouteState : TripState
{
    public static readonly EnRouteState Instance = new();
    private EnRouteState() { }

    public override TripStatus Status => TripStatus.EnRoute;
    public override TripState PickUp() => PickedUpState.Instance;
    public override TripState Cancel() => CancelledState.Instance;   // rider vẫn hủy được khi chưa đón khách
}