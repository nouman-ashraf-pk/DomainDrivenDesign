namespace OrderManagement.Domain.Common;

// An Aggregate is a cluster of entities/value objects treated as a single unit
// for the purpose of data changes. The AggregateRoot is the only entry point:
// outside code is never allowed to reach into the cluster and mutate a child
// directly (e.g. you never call order.Items[0].SetQuantity(5) from outside —
// you call order.ChangeItemQuantity(...) and the root enforces the rules).
//
// This is where invariants are enforced and domain events are collected so
// that infrastructure (or an in-process mediator) can dispatch them after the
// transaction commits.
public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() { }
    protected AggregateRoot(TId id) : base(id) { }

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
