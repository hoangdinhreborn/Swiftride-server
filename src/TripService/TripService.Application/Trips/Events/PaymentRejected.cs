namespace TripService.Application.Trips.Events;

public static class TripRoutingKeys
{
    public const string PaymentRejected = "trip.payment-rejected";
    public const string Accepted = "trip.accepted";
}

// Trip không áp dụng được một thanh toán đã trừ tiền: yêu cầu Payment hoàn lại (bồi thường)
public record PaymentRejected(Guid EventId, Guid PaymentId, Guid TripId, string Reason, DateTime OccurredAt);