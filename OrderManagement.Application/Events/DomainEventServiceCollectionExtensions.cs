using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Domain.Common;

namespace OrderManagement.Application.Events;

public static class DomainEventServiceCollectionExtensions
{
    // Registers the dispatcher plus every IDomainEventHandler<T> implementation
    // found in the given assembly, so adding a new handler is just "write the
    // class" - nobody has to remember to also edit Program.cs.
    public static IServiceCollection AddDomainEventHandling(this IServiceCollection services, Assembly handlersAssembly)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        var handlerInterface = typeof(IDomainEventHandler<>);

        var candidateTypes = handlersAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false });

        foreach (var type in candidateTypes)
        {
            var implementedHandlerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface);

            foreach (var implementedInterface in implementedHandlerInterfaces)
                services.AddScoped(implementedInterface, type);
        }

        return services;
    }
}