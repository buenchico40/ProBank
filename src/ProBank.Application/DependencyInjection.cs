using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ProBank.Application.Abstractions;

namespace ProBank.Application;

public static class DependencyInjection
{
    /// <summary>Registra el despachador y todos los handlers de comandos y consultas del ensamblado.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDispatcher, Dispatcher>();
        var handlerTypes = new[] { typeof(ICommandHandler<,>), typeof(IQueryHandler<,>) };
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
        foreach (var itf in type.GetInterfaces().Where(i => i.IsGenericType && handlerTypes.Contains(i.GetGenericTypeDefinition())))
            services.AddScoped(itf, type);
        return services;
    }
}
