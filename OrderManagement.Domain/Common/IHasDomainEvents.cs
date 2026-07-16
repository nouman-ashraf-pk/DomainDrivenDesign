namespace OrderManagement.Domain.Common;

// AggregateRoot<TId> is generic, which means EF Core's ChangeTracker can't
// ask "give me every tracked entity with pending domain events" in one query
// - it would need to know TId in advance. This non-generic interface is the
// seam that lets infrastructure code (AppDbContext.SaveChangesAsync) find
// ANY aggregate with events, regardless of its id type, without the Domain
// layer knowing anything about EF Core.
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
