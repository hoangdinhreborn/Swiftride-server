using MatchingService.Application.Drivers.Interfaces;
using MatchingService.Infrastructure.Persistence;
using MatchingService.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace MatchingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString, string databaseName)
    {
        services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName));
        services.AddSingleton<MatchingMongoContext>();
        services.AddScoped<IDriverLocationRepository, DriverLocationRepository>();
        return services;
    }
}