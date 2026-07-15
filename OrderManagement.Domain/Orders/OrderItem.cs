using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Orders;

// OrderItem is an Entity, not a Value Object: two lines with the same product,
// price and quantity are still distinct rows in this order. But it is NOT an
// aggregate root — nothing outside Order is allowed to load or save an
// OrderItem directly. That's why the constructor is internal.
public sealed class OrderItem : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public Money UnitPrice { get; private set; } = default!;
    public int Quantity { get; private set; }

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    private OrderItem() { } // EF Core

    internal OrderItem(Guid productId, string productName, Money unitPrice, int quantity)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Product name is required.");
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    internal void ChangeQuantity(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be greater than zero.");
        Quantity = quantity;
    }
}
