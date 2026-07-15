namespace OrderManagement.Domain.Orders;

// The DOMAIN defines the contract; INFRASTRUCTURE provides the implementation
// (EF Core, Dapper, in-memory, whatever). This is the Dependency Inversion
// Principle in action: Domain never references Infrastructure.
//
// Notice there's no GetAll(), no IQueryable<Order>, no generic filter builder.
// A repository should offer only the operations the aggregate's use cases
// actually need — it is not a generic data-access dumping ground.
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    void Update(Order order);
}

// Unit of Work: one transaction, one SaveChanges. The application layer decides
// when to commit; the domain layer never calls SaveChanges itself.
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
