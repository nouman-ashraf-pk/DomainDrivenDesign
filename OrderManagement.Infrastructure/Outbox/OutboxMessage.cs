using System.Text.Json;
using OrderManagement.Domain.Common;

namespace OrderManagement.Infrastructure.Outbox;

// A durability record, NOT a domain concept - this is why it lives in
// Infrastructure and not Domain. Its whole job is: "guarantee that a domain
// event which was true enough to save to the database also eventually gets
// dispatched," even across a process crash between those two moments.
//
// Written in the SAME transaction as the aggregate that raised the event
// (see AppDbContext.SaveChangesAsync), then picked up later by
// OutboxProcessor and handed to the same IDomainEventDispatcher the
// in-process path already used - the handlers don't know or care whether
// they were invoked synchronously or from the outbox.
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }

    // Domain.Orders.OrderPlacedEvent, etc. - resolved back to a real Type by
    // DomainEventTypeResolver when the processor reads this row.
    public string Type { get; private set; } = default!;

    public string Payload { get; private set; } = default!;
    public DateTime OccurredOn { get; private set; }

    // Null = not yet dispatched. Set once every handler has succeeded.
    public DateTime? ProcessedOn { get; private set; }

    // Last failure, if any - kept for visibility/alerting; the row stays
    // unprocessed (and gets retried) as long as this is set but ProcessedOn isn't.
    public string? Error { get; private set; }
    public int Attempts { get; private set; }

    private OutboxMessage() { } // EF Core

    public OutboxMessage(IIntegrationEvent domainEvent)
    {
        Id = Guid.NewGuid();
        Type = domainEvent.GetType().FullName!;
        Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
        OccurredOn = domainEvent.OccurredOn;
    }

    public IIntegrationEvent Deserialize()
    {
        var eventType = DomainEventTypeResolver.Resolve(Type);
        return (IIntegrationEvent)JsonSerializer.Deserialize(Payload, eventType)!;
    }

    public void MarkProcessed() => ProcessedOn = DateTime.UtcNow;

    public void MarkFailed(string error)
    {
        Attempts++;
        Error = error;
    }
}
