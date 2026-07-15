using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Orders;

namespace OrderManagement.Infrastructure.Persistence;

public sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context) => _context = context;

    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default) =>
        await _context.Orders
            .Include("_items") // load the owned collection via its backing field
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task AddAsync(Order order, CancellationToken ct = default) =>
        await _context.Orders.AddAsync(order, ct);

    // Aggregates loaded by EF are already tracked, so "Update" is usually a
    // no-op call kept for symmetry/readability at the call site — SaveChanges
    // picks up tracked changes automatically. It matters more with detached
    // entities (e.g. coming back from a distributed cache).
    public void Update(Order order) => _context.Orders.Update(order);
}
