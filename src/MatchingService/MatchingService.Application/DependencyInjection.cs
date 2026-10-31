using MatchingService.Application.Common.Interfaces;
using MatchingService.Domain.Pricing;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var handlerType = typeof(IHandler<,>);

        var types = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false });

        foreach (var type in types)
            foreach (var i in type.GetInterfaces()
                         .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType))
                services.AddScoped(i, type);

        services.AddSingleton<FareCalculator>();   // FareRates do Api đăng ký
        return services;
    }
}