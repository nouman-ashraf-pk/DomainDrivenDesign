using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderManagement.Domain.Common;

namespace OrderManagement.Application.Events;

// The one piece of "magic" in the event pipeline: for each event, build the
// closed generic IDomainEventHandler<TheConcreteEventType>, ask the DI
// container for every registration of it, and await them one by one.
//
// Deliberately NOT parallel and NOT swallowing exceptions: if a handler
// throws, the whole use case fails and nothing is silently dropped. That's
// the right default for an in-process, same-transaction dispatcher. If you
// later need "fire and forget" or "survive a handler failure", that's a sign
// you want an outbox + background processor instead of this.
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

            var handlers = _serviceProvider.GetServices(handlerType).ToList();
            if (handlers.Count == 0)
            {
                _logger.LogDebug("No handlers registered for {Event}; skipping.", eventType.Name);
                continue;
            }

            foreach (var handler in handlers)
            {
                ct.ThrowIfCancellationRequested();

                // We only have the handler as `object` (the DI container was
                // asked for a non-generic Type), so we get to `Task HandleAsync(...)`
                // through reflection rather than a compile-time cast.
                var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
                var task = (Task)method.Invoke(handler, new object[] { domainEvent, ct })!;
                await task;
            }
        }
    }
}
