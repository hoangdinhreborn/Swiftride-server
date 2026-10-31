using Microsoft.EntityFrameworkCore;
using TripService.Application.Common.Exceptions;
using TripService.Application.Common.Interfaces;
using TripService.Domain.Trips;

namespace TripService.Infrastructure.Persistence;

public class TripDbContext(DbContextOptions<TripDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Trip> Trips => Set<Trip>();

    protected override void OnModelCreating(ModelBuilder b)
        => b.ApplyConfigurationsFromAssembly(typeof(TripDbContext).Assembly);

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try { return await base.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException("Chuyến đi vừa bị người khác thay đổi, hãy thử lại");
        }
    }
}