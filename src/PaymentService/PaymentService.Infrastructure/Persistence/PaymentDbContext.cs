using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PaymentService.Application.Common.Exceptions;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Payments;
using PaymentService.Infrastructure.Persistence.Outbox;
using System.Diagnostics;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options)
    : DbContext(options), IUnitOfWork, IOutbox
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
        => b.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);

    public void Enqueue(string routingKey, object message)
        => OutboxMessages.Add(new OutboxMessage
        {
            RoutingKey = routingKey,
            Type = message.GetType().Name,
            Payload = JsonSerializer.Serialize(message, message.GetType(), JsonSerializerOptions.Web),
            TraceParent = Activity.Current?.Id
        });

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException("Thanh toán vừa bị thay đổi, hãy thử lại");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConcurrencyException("Yêu cầu thanh toán trùng đang được xử lý, hãy thử lại sau");
        }
    }
}