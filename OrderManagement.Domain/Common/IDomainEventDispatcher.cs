namespace OrderManagement.Domain.Common;

// Takes the events an aggregate collected while it was being used and hands
// each one to whichever handlers are registered for it. The Domain only
// needs the contract; the actual "how do I find the handlers" logic (DI
// container lookup, reflection, etc.) belongs in Application/Infrastructure.
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default);
}

public interface IIntegrationEventDispatcher
{
    Task DispatchAsync(IEnumerable<IIntegrationEvent> integrationEvents, CancellationToken ct = default);
}

