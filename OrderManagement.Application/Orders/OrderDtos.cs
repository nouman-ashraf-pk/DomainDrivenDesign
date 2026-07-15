namespace OrderManagement.Application.Orders;

// DTOs are the boundary between the outside world and the domain model.
// Controllers never see Order, OrderItem, or Money directly — only these
// flat, serialization-friendly shapes. This keeps the domain model free to
// change internally without breaking API contracts, and stops EF/JSON
// concerns from leaking into domain code.
public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    string Status,
    decimal Total,
    string Currency,
    IReadOnlyList<OrderItemDto> Items,
    DateTime CreatedAt);

public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public sealed record CreateOrderRequest(
    Guid CustomerId,
    string Street,
    string City,
    string PostalCode,
    string Country,
    string Currency = "USD");

public sealed record AddItemRequest(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);

public sealed record ShipOrderRequest(string Carrier, string TrackingNumber);

public sealed record CancelOrderRequest(string Reason);
