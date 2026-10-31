using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Payments.Interfaces;
using PaymentService.Domain.Payments;

namespace PaymentService.Infrastructure.Persistence.Repositories;

public class PaymentRepository(PaymentDbContext db) : IPaymentRepository
{
    private IQueryable<Payment> Query => db.Payments.Include(p => p.Entries);

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Query.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Payment?> GetByKeyAsync(Guid payerId, string idempotencyKey, CancellationToken ct = default)
        => Query.FirstOrDefaultAsync(p => p.PayerId == payerId && p.IdempotencyKey == idempotencyKey, ct);

    public Task<Payment?> GetActiveByTripAsync(Guid tripId, CancellationToken ct = default)
        => Query.FirstOrDefaultAsync(p => p.TripId == tripId && p.Status != PaymentStatus.Failed, ct);

    public async Task AddAsync(Payment payment, CancellationToken ct = default)
        => await db.Payments.AddAsync(payment, ct);
}