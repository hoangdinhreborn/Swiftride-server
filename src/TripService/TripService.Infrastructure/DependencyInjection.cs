using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Interfaces;
using TripService.Infrastructure.Gateways;
using TripService.Infrastructure.Messaging;
using TripService.Infrastructure.Persistence;
using TripService.Infrastructure.Persistence.Repositories;

namespace TripService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString, string matchingBaseUrl, string rabbitUri)
    {
        services.AddDbContext<TripDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TripDbContext>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TripDbContext>());
        services.AddScoped<IOutbox>(sp => sp.GetRequiredService<TripDbContext>());   // cùng một DbContext => cùng transaction
        services.AddScoped<ITripRepository, TripRepository>();

        services.AddTransient<BearerTokenHandler>();
        services.AddHttpClient<IPricingGateway, MatchingPricingGateway>(c =>
            {
                c.BaseAddress = new Uri(matchingBaseUrl);
                c.Timeout = TimeSpan.FromSeconds(3);
            })
            .AddHttpMessageHandler<BearerTokenHandler>();
        services.AddHttpClient<IDriverMatchingGateway, MatchingDriverGateway>(c =>
            {
                c.BaseAddress = new Uri(matchingBaseUrl);
                c.Timeout = TimeSpan.FromSeconds(3);
            })
    .AddHttpMessageHandler<BearerTokenHandler>();
        services.AddSingleton<IRawPublisher>(new RabbitMqPublisher(rabbitUri));
        services.AddHostedService<OutboxPublisher>();
        services.AddSingleton(new RabbitMqSettings(rabbitUri));
        services.AddHostedService<PaymentResultConsumer>();

        return services;
    }
}