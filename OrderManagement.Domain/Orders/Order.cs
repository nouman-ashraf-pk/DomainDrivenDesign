using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Orders;

// The Aggregate Root. Everything below this line is the "consistency
// boundary": whatever rules apply to an order (must have items to be placed,
// can't ship before paid, etc.) are enforced HERE, in one place, and can never
// be bypassed by reaching into Items directly — the setter is private and the
// list is exposed read-only.
public sealed class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderItem> _items = new();

    public Guid CustomerId { get; private set; }
    public Address ShippingAddress { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public string Currency { get; private set; } = "USD";
    public DateTime CreatedAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public Money Total => _items
        .Select(i => i.LineTotal)
        .Aggregate(Money.Zero(Currency), (sum, line) => sum.Add(line));

    private Order() { } // EF Core

    private Order(OrderId id, Guid customerId, Address shippingAddress, string currency)
        : base(id)
    {
        CustomerId = customerId;
        ShippingAddress = shippingAddress;
        Currency = currency;
        Status = OrderStatus.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    // Factory method instead of a public constructor: guarantees you can never
    // hold a half-built, invalid Order in memory.
    public static Order StartDraft(Guid customerId, Address shippingAddress, string currency = "USD")
    {
        if (customerId == Guid.Empty) throw new DomainException("Customer id is required.");
        return new Order(OrderId.New(), customerId, shippingAddress, currency);
    }

    public void AddItem(Guid productId, string productName, Money unitPrice, int quantity)
    {
        EnsureDraft("add items to");

        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
        {
            existing.ChangeQuantity(existing.Quantity + quantity);
            return;
        }

        _items.Add(new OrderItem(productId, productName, unitPrice, quantity));
    }

    public void RemoveItem(Guid productId)
    {
        EnsureDraft("remove items from");
        var item = _items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new DomainException("Item not found on this order.");
        _items.Remove(item);
    }

    public void Place()
    {
        EnsureDraft("place");
        if (_items.Count == 0)
            throw new DomainException("Cannot place an order with no items.");
        if (this.Total.Amount > 10000)
        {
            Status = OrderStatus.ManagerReview;
            Raise(new OrderPendingApprovalEvent(Id, CustomerId, Total));
            return;
        }
            
        Status = OrderStatus.Placed;
        Raise(new OrderPlacedEvent(Id, CustomerId, Total));
    }

    public void MarkAsPaid()
    {
        if (Status != OrderStatus.Placed)
            throw new DomainException($"Cannot mark as paid from status {Status}.");

        Status = OrderStatus.Paid;
        Raise(new OrderPaidEvent(Id));
    }

    public void Ship(string carrier, string trackingNumber)
    {
        if (Status != OrderStatus.Paid)
            throw new DomainException($"Cannot ship an order in status {Status}; it must be Paid first.");
        if (string.IsNullOrWhiteSpace(carrier)) throw new DomainException("Carrier is required.");
        if (string.IsNullOrWhiteSpace(trackingNumber)) throw new DomainException("Tracking number is required.");

        Status = OrderStatus.Shipped;
        Raise(new OrderShippedEvent(Id, carrier, trackingNumber));
    }

    public void Cancel(string reason)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Cancelled)
            throw new DomainException($"Cannot cancel an order in status {Status}.");

        Status = OrderStatus.Cancelled;
        CancellationReason = reason;
        Raise(new OrderCancelledEvent(Id, reason));
    }

    public void Approve()
    {
        if(Status != OrderStatus.ManagerReview)
            throw new DomainException($"Cannot approve an order in status {Status}");
        Status = OrderStatus.Placed;
        Raise(new OrderPlacedEvent(Id, CustomerId, Total));

    }

    public void Reject(string reason)
    {
        if (Status != OrderStatus.ManagerReview)
            throw new DomainException($"Cannot reject an order in status {Status}");
        Status = OrderStatus.Cancelled;
        CancellationReason = reason;
        Raise(new OrderCancelledEvent(Id, reason));
    }

    private void EnsureDraft(string action)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException($"Cannot {action} an order once it has left Draft status (current: {Status}).");
    }
}
