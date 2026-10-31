using PaymentService.Domain.Payments;

namespace PaymentService.Application.Payments.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Payment?> GetByKeyAsync(Guid payerId, string idempotencyKey, CancellationToken ct = default);
    Task<Payment?> GetActiveByTripAsync(Guid tripId, CancellationToken ct = default);   // chưa Failed
    Task AddAsync(Payment payment, CancellationToken ct = default);
}