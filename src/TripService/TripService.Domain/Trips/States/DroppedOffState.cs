namespace TripService.Domain.Trips.States;

internal sealed class DroppedOffState : TripState
{
    public static readonly DroppedOffState Instance = new();
    private DroppedOffState() { }

    public override TripStatus Status => TripStatus.DroppedOff;
    public override TripState MarkPaid() => PaidState.Instance;
    public override TripState MarkPaymentPending() => PaymentPendingState.Instance;
}
