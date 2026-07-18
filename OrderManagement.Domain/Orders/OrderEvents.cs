using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Orders;

public sealed record OrderPlacedEvent(OrderId OrderId, Guid CustomerId, Money Total) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record OrderPaidEvent(OrderId OrderId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record OrderShippedEvent(OrderId OrderId, string Carrier, string TrackingNumber) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record OrderCancelledEvent(OrderId OrderId, string Reason) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


public sealed record OrderPendingApprovalEvent(OrderId OrderId, Guid CustomerId, Money Total) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record OrderUpdateEvent(OrderId OrderId, Guid CustomerId, Money Total) : IIntegrationEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    public Guid Id => throw new NotImplementedException();
}