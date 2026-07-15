using Microsoft.Extensions.Logging;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Orders;

namespace OrderManagement.Application.Orders.EventHandlers;

// Each handler does exactly one job and knows nothing about the others.
// That's the point of moving off the old "foreach event { log it }" code:
// OrderPlacedEvent can now fan out to as many independent reactions as the
// business needs (email, inventory, analytics...) without OrderService or
// the Order aggregate ever finding out about any of them.

// --- OrderPlacedEvent -------------------------------------------------

public sealed class SendOrderConfirmationEmailHandler : IDomainEventHandler<OrderPlacedEvent>
{
    private readonly ILogger<SendOrderConfirmationEmailHandler> _logger;

    public SendOrderConfirmationEmailHandler(ILogger<SendOrderConfirmationEmailHandler> logger) => _logger = logger;

    public Task HandleAsync(OrderPlacedEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Sending order confirmation email to customer {CustomerId} for order {OrderId} (total {Total})",
            domainEvent.CustomerId, domainEvent.OrderId, domainEvent.Total);
        return Task.CompletedTask;
    }
}

public sealed class ReserveInventoryHandler : IDomainEventHandler<OrderPlacedEvent>
{
    private readonly ILogger<ReserveInventoryHandler> _logger;

    public ReserveInventoryHandler(ILogger<ReserveInventoryHandler> logger) => _logger = logger;

    public Task HandleAsync(OrderPlacedEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Reserving inventory for order {OrderId}", domainEvent.OrderId);
        return Task.CompletedTask;
    }
}

// --- OrderPaidEvent -----------------------------------------------------

public sealed class NotifyFulfillmentHandler : IDomainEventHandler<OrderPaidEvent>
{
    private readonly ILogger<NotifyFulfillmentHandler> _logger;

    public NotifyFulfillmentHandler(ILogger<NotifyFulfillmentHandler> logger) => _logger = logger;

    public Task HandleAsync(OrderPaidEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Notifying fulfillment center that order {OrderId} has been paid and is ready to pack",
            domainEvent.OrderId);
        return Task.CompletedTask;
    }
}

// --- OrderShippedEvent ---------------------------------------------------

public sealed class SendShippingNotificationHandler : IDomainEventHandler<OrderShippedEvent>
{
    private readonly ILogger<SendShippingNotificationHandler> _logger;

    public SendShippingNotificationHandler(ILogger<SendShippingNotificationHandler> logger) => _logger = logger;

    public Task HandleAsync(OrderShippedEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Sending shipping notification for order {OrderId}: carrier {Carrier}, tracking {TrackingNumber}",
            domainEvent.OrderId, domainEvent.Carrier, domainEvent.TrackingNumber);
        return Task.CompletedTask;
    }
}

// --- OrderCancelledEvent --------------------------------------------------

public sealed class ReleaseInventoryHandler : IDomainEventHandler<OrderCancelledEvent>
{
    private readonly ILogger<ReleaseInventoryHandler> _logger;

    public ReleaseInventoryHandler(ILogger<ReleaseInventoryHandler> logger) => _logger = logger;

    public Task HandleAsync(OrderCancelledEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Releasing any reserved inventory for cancelled order {OrderId} (reason: {Reason})",
            domainEvent.OrderId, domainEvent.Reason);
        return Task.CompletedTask;
    }
}

public sealed class SendCancellationEmailHandler : IDomainEventHandler<OrderCancelledEvent>
{
    private readonly ILogger<SendCancellationEmailHandler> _logger;

    public SendCancellationEmailHandler(ILogger<SendCancellationEmailHandler> logger) => _logger = logger;

    public Task HandleAsync(OrderCancelledEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Sending cancellation email for order {OrderId} (reason: {Reason})",
            domainEvent.OrderId, domainEvent.Reason);
        return Task.CompletedTask;
    }
}

// --- OrderPendingApprovalEvent --------------------------------------------

public sealed class NotifyManagerForApprovalHandler : IDomainEventHandler<OrderPendingApprovalEvent>
{
    private readonly ILogger<NotifyManagerForApprovalHandler> _logger;

    public NotifyManagerForApprovalHandler(ILogger<NotifyManagerForApprovalHandler> logger) => _logger = logger;

    public Task HandleAsync(OrderPendingApprovalEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Order {OrderId} for customer {CustomerId} exceeds the auto-approval threshold (total {Total}) " +
            "- notifying a manager for review",
            domainEvent.OrderId, domainEvent.CustomerId, domainEvent.Total);
        return Task.CompletedTask;
    }
}
