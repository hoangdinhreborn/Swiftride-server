namespace TripService.Domain.Trips;

public enum TripStatus
{
    Requested,        // đã báo giá, chờ rider xác nhận
    Searching,        // đang gửi yêu cầu tới Matching (chỉ tồn tại trong một request, không lưu DB)
    DriverOffered,    // đã đề xuất cho một tài xế
    DriverAccepted, PickedUp, DroppedOff, PaymentPending, Paid,
    Cancelled,        // rider từ chối giá hoặc hủy
    Failed            // không có tài xế, hoặc bị từ chối quá k lần
}