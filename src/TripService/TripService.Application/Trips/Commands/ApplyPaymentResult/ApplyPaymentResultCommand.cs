namespace TripService.Application.Trips.Commands.ApplyPaymentResult;

public record ApplyPaymentResultCommand(Guid TripId, Guid PaymentId, bool Succeeded);

// Applied = false nghĩa là Trip từ chối áp dụng (khác với lỗi hạ tầng, vốn ném exception)
public record ApplyPaymentResultOutcome(bool Applied, string TripStatus, string? RejectionReason = null);