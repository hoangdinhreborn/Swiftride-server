using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Payments.Interfaces;
using PaymentService.Infrastructure.Gateways;
using PaymentService.Infrastructure.Messaging;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.Persistence.Repositories;
using PaymentService.Infrastructure.Providers;

namespace PaymentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString, string tripBaseUrl, string rabbitUri)
    {
        services.AddDbContext<PaymentDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PaymentDbContext>());
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddSingleton<IPaymentProvider, FakePaymentProvider>();

        services.AddTransient<BearerTokenHandler>();
        services.AddHttpClient<ITripGateway, TripHttpGateway>(c =>
            {
                c.BaseAddress = new Uri(tripBaseUrl);
                c.Timeout = TimeSpan.FromSeconds(3);
            })
            .AddHttpMessageHandler<BearerTokenHandler>();
        services.AddSingleton(new RabbitMqSettings(rabbitUri));
        services.AddHostedService<RideCompletedConsumer>();
        services.AddScoped<IOutbox>(sp => sp.GetRequiredService<PaymentDbContext>());   // cùng DbContext => cùng transaction
        services.AddSingleton<IRawPublisher>(new RabbitMqPublisher(rabbitUri));
        services.AddHostedService<OutboxPublisher>();

        return services;
    }
}