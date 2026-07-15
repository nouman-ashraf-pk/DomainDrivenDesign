using Microsoft.Extensions.Logging;
using OrderManagement.Domain.Orders;

namespace OrderManagement.Application.Orders;

// The Application layer is deliberately "thin" and boring: each method is a
// use case that (1) loads the aggregate, (2) tells it to do something,
// (3) persists it, (4) maps the result to a DTO. If you find yourself writing
// an if/else business rule here, it belongs on the Order aggregate instead.
public sealed class OrderService
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IOrderRepository orders, IUnitOfWork unitOfWork, ILogger<OrderService> logger)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<OrderDto> CreateDraftAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var address = Address.Of(request.Street, request.City, request.PostalCode, request.Country);
        var order = Order.StartDraft(request.CustomerId, address, request.Currency);

        await _orders.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ToDto(order);
    }

    public async Task<OrderDto> AddItemAsync(Guid orderId, AddItemRequest request, CancellationToken ct = default)
    {
        var order = await GetOrThrow(orderId, ct);

        order.AddItem(request.ProductId, request.ProductName, Money.Of(request.UnitPrice, order.Currency), request.Quantity);

        _orders.Update(order);
        await _unitOfWork.SaveChangesAsync(ct);
        return ToDto(order);
    }

    public async Task<OrderDto> PlaceAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await GetOrThrow(orderId, ct);
        order.Place();
        await PersistAndDispatch(order, ct);
        return ToDto(order);
    }

    public async Task<OrderDto> MarkAsPaidAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await GetOrThrow(orderId, ct);
        order.MarkAsPaid();
        await PersistAndDispatch(order, ct);
        return ToDto(order);
    }

    public async Task<OrderDto> ShipAsync(Guid orderId, ShipOrderRequest request, CancellationToken ct = default)
    {
        var order = await GetOrThrow(orderId, ct);
        order.Ship(request.Carrier, request.TrackingNumber);
        await PersistAndDispatch(order, ct);
        return ToDto(order);
    }

    public async Task<OrderDto> CancelAsync(Guid orderId, CancelOrderRequest request, CancellationToken ct = default)
    {
        var order = await GetOrThrow(orderId, ct);
        order.Cancel(request.Reason);
        await PersistAndDispatch(order, ct);
        return ToDto(order);
    }

    public async Task<OrderDto> ApproveAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await GetOrThrow(orderId, ct);
        order.Approve();
        await PersistAndDispatch(order, ct);
        return ToDto(order);
    }

    public async Task<OrderDto> RejectAsync(Guid orderId, string reason, CancellationToken ct = default)
    {
        var order = await GetOrThrow(orderId, ct);
        order.Reject(reason);
        await PersistAndDispatch(order, ct);
        return ToDto(order);
    }

    public async Task<OrderDto?> GetAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _orders.GetByIdAsync(OrderId.Of(orderId), ct);
        return order is null ? null : ToDto(order);
    }

    private async Task<Order> GetOrThrow(Guid orderId, CancellationToken ct)
    {
        return await _orders.GetByIdAsync(OrderId.Of(orderId), ct)
            ?? throw new KeyNotFoundException($"Order {orderId} was not found.");
    }

    // Naive in-process "dispatch": just log. In a real system this is where
    // you'd publish to an outbox, a message bus, or run in-process handlers
    // (e.g. send a confirmation email when OrderPlacedEvent fires).
    private async Task PersistAndDispatch(Order order, CancellationToken ct)
    {
        _orders.Update(order);
        await _unitOfWork.SaveChangesAsync(ct);

        foreach (var domainEvent in order.DomainEvents)
            _logger.LogInformation("Domain event: {Event}", domainEvent);

        order.ClearDomainEvents();
    }

    private static OrderDto ToDto(Order order) => new(
        order.Id.Value,
        order.CustomerId,
        order.Status.ToString(),
        order.Total.Amount,
        order.Total.Currency,
        order.Items.Select(i => new OrderItemDto(
            i.ProductId, i.ProductName, i.UnitPrice.Amount, i.Quantity, i.LineTotal.Amount)).ToList(),
        order.CreatedAt);
}
