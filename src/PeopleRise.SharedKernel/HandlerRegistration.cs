using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace PeopleRise.SharedKernel;

/// <summary>Scans an assembly and registers every command/query handler as scoped — both by its
/// concrete type (for direct injection into endpoints) and by its handler interface (for any future
/// dispatcher/pipeline). Adding a new handler needs no registration code.</summary>
public static class HandlerRegistration
{
    private static readonly Type[] HandlerInterfaces = [typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)];

    public static IServiceCollection AddHandlersFromAssemblyContaining<TMarker>(this IServiceCollection services) =>
        services.AddHandlersFromAssembly(typeof(TMarker).Assembly);

    public static IServiceCollection AddHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            var handlerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && HandlerInterfaces.Contains(i.GetGenericTypeDefinition()))
                .ToArray();
            if (handlerInterfaces.Length == 0) continue;

            services.AddScoped(type);                                   // inject the concrete handler
            foreach (var i in handlerInterfaces) services.AddScoped(i, type);   // …and via its interface
        }
        return services;
    }
}
