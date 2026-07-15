namespace OrderManagement.Domain.Common;

// Anything that reacts to a domain event implements this. There can be zero,
// one, or many handlers for the same event type — e.g. OrderPlacedEvent might
// be handled by both "send confirmation email" and "notify the warehouse".
// Handlers live in the Application (or Infrastructure) layer, never here —
// this interface is the only thing the Domain needs to know about them.
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct = default);
}
