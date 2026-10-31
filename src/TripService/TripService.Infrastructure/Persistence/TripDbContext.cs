using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TripService.Application.Common.Exceptions;
using TripService.Application.Common.Interfaces;
using TripService.Domain.Trips;
using TripService.Infrastructure.Persistence.Outbox;
using System.Diagnostics;

namespace TripService.Infrastructure.Persistence;

public class TripDbContext(DbContextOptions<TripDbContext> options)
    : DbContext(options), IUnitOfWork, IOutbox
{
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
        => b.ApplyConfigurationsFromAssembly(typeof(TripDbContext).Assembly);

    public void Enqueue(string routingKey, object message)
        => OutboxMessages.Add(new OutboxMessage
        {
            RoutingKey = routingKey,
            Type = message.GetType().Name,
            Payload = JsonSerializer.Serialize(message, message.GetType(), JsonSerializerOptions.Web),
            TraceParent = Activity.Current?.Id    // trace của request đang xử lý
        });

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException("Chuyến đi vừa bị người khác thay đổi, hãy thử lại");
        }
    }
}