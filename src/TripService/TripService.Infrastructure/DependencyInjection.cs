using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Interfaces;
using TripService.Infrastructure.Persistence;
using TripService.Infrastructure.Persistence.Repositories;

namespace TripService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<TripDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TripDbContext>());
        services.AddScoped<ITripRepository, TripRepository>();
        return services;
    }
}