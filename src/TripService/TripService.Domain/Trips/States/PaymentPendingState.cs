namespace TripService.Domain.Trips.States;

internal sealed class PaymentPendingState : TripState
{
    public static readonly PaymentPendingState Instance = new();
    private PaymentPendingState() { }

    public override TripStatus Status => TripStatus.PaymentPending;
    public override TripState MarkPaid() => PaidState.Instance;                 // rider thử lại và thành công
    public override TripState MarkPaymentPending() => this;                     // sự kiện thất bại đến lần hai: bỏ qua
}
