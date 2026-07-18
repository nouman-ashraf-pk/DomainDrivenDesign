namespace OrderManagement.Domain.Common;

// A fact that happened inside the domain and that other parts of the system
// (or other bounded contexts) might care about — e.g. "an order was placed."
// Note it's named in the past tense: it already happened.
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}


// Application layer (or a new Integration namespace) — crosses process boundaries
public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
}