using Microsoft.Extensions.DependencyInjection;
using TripService.Application.Common.Interfaces;

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

        return services;
    }
}