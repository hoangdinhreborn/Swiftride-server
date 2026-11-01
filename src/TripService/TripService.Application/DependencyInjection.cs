using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips;

namespace TripService.Application;

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

        services.AddScoped<DriverDispatcher>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}