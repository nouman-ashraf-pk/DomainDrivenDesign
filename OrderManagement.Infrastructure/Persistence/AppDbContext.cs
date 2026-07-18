using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Orders;
using OrderManagement.Infrastructure.Outbox;

namespace OrderManagement.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }


    // The one place ALL domain events get captured, for ANY aggregate, without
    // OrderService (or any future application service) having to remember to
    // do it. This runs inside the same SaveChanges call - and therefore the
    // same DB transaction - as the aggregate's own changes, which is the
    // entire point: either the order update AND its outbox row both land, or
    // neither does. There's no window where one is saved and the other isn't.
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregatesWithEvents = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregatesWithEvents)
        {
            // Clear immediately, not after dispatch: dispatch now happens
            // later, out-of-process (OutboxProcessor), so there's nothing
            // left for this aggregate instance to hold onto. The outbox row
            // is now the durable record of "this event happened."
            aggregate.ClearDomainEvents();
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
