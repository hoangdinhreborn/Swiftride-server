namespace TripService.Domain.Trips.States;

internal sealed class DriverAcceptedState : TripState
{
    public static readonly DriverAcceptedState Instance = new();
    private DriverAcceptedState() { }

    public override TripStatus Status => TripStatus.DriverAccepted;
    public override TripState PickUp() => PickedUpState.Instance;
    public override TripState Cancel() => CancelledState.Instance;
}
