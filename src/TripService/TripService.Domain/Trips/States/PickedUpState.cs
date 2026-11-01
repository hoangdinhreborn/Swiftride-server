namespace TripService.Domain.Trips.States;

internal sealed class PickedUpState : TripState
{
    public static readonly PickedUpState Instance = new();
    private PickedUpState() { }

    public override TripStatus Status => TripStatus.PickedUp;
    public override TripState DropOff() => DroppedOffState.Instance;
}
