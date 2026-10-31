namespace PaymentService.Application.Payments.Interfaces;

public record TripInfo(Guid Id, Guid RiderId, string Status, decimal QuotedFare);

public interface ITripGateway
{
    Task<TripInfo?> GetTripAsync(Guid tripId, CancellationToken ct = default);   // null nếu không có
}