namespace TripService.Domain.Trips.States;

// Broker giao at-least-once và có thể sai thứ tự, nên Paid nhận lại các kết quả cũ mà không đổi gì
internal sealed class PaidState : TripState
{
    public static readonly PaidState Instance = new();
    private PaidState() { }

    public override TripStatus Status => TripStatus.Paid;
    public override TripState MarkPaid() => this;
    public override TripState MarkPaymentPending() => this;
}
