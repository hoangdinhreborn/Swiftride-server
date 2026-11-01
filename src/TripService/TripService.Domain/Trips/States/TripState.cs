using TripService.Domain.Common;

namespace TripService.Domain.Trips.States;

// Mặc định mọi hành động đều bị từ chối. Mỗi state con chỉ ghi đè những hành động hợp lệ của nó
internal abstract class TripState
{
    public abstract TripStatus Status { get; }

    public virtual TripState Confirm() => Reject("xác nhận giá");
    public virtual TripState OfferTo() => Reject("đề xuất cho tài xế");
    public virtual TripState NoDriverFound() => Reject("kết thúc vì không có tài xế");
    public virtual TripState Accept() => Reject("nhận chuyến");
    public virtual TripState Decline(int declineCount) => Reject("từ chối chuyến");
    public virtual TripState PickUp() => Reject("đón khách");
    public virtual TripState DropOff() => Reject("trả khách");
    public virtual TripState MarkPaid() => Reject("ghi nhận thanh toán");
    public virtual TripState MarkPaymentPending() => Reject("ghi nhận thanh toán thất bại");
    public virtual TripState Cancel() => Reject("hủy chuyến");

    private TripState Reject(string action)
        => throw new DomainException($"Không thể {action} khi chuyến đang ở trạng thái {Status}");

    public static TripState For(TripStatus status) => status switch
    {
        TripStatus.Requested      => RequestedState.Instance,
        TripStatus.Searching      => SearchingState.Instance,
        TripStatus.DriverOffered  => DriverOfferedState.Instance,
        TripStatus.DriverAccepted => DriverAcceptedState.Instance,
        TripStatus.PickedUp       => PickedUpState.Instance,
        TripStatus.DroppedOff     => DroppedOffState.Instance,
        TripStatus.PaymentPending => PaymentPendingState.Instance,
        TripStatus.Paid           => PaidState.Instance,
        TripStatus.Cancelled      => CancelledState.Instance,
        TripStatus.Failed         => FailedState.Instance,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Trạng thái chưa có State tương ứng")
    };
}