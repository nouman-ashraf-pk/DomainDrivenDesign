using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Orders;
using OrderManagement.Domain.Common;

namespace OrderManagement.Api.Controllers;

// The controller's only jobs: parse HTTP -> call the application service ->
// map the result/exception to an HTTP response. No business logic here.
[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService) => _orderService = orderService;

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var order = await _orderService.CreateDraftAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { orderId = order.Id }, order);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<OrderDto>> Get(Guid orderId, CancellationToken ct)
    {
        var order = await _orderService.GetAsync(orderId, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost("{orderId:guid}/items")]
    public Task<ActionResult<OrderDto>> AddItem(Guid orderId, AddItemRequest request, CancellationToken ct) =>
        Handle(() => _orderService.AddItemAsync(orderId, request, ct));

    [HttpPost("{orderId:guid}/place")]
    public Task<ActionResult<OrderDto>> Place(Guid orderId, CancellationToken ct) =>
        Handle(() => _orderService.PlaceAsync(orderId, ct));

    [HttpPost("{orderId:guid}/pay")]
    public Task<ActionResult<OrderDto>> Pay(Guid orderId, CancellationToken ct) =>
        Handle(() => _orderService.MarkAsPaidAsync(orderId, ct));

    [HttpPost("{orderId:guid}/ship")]
    public Task<ActionResult<OrderDto>> Ship(Guid orderId, ShipOrderRequest request, CancellationToken ct) =>
        Handle(() => _orderService.ShipAsync(orderId, request, ct));

    [HttpPost("{orderId:guid}/cancel")]
    public Task<ActionResult<OrderDto>> Cancel(Guid orderId, CancelOrderRequest request, CancellationToken ct) =>
        Handle(() => _orderService.CancelAsync(orderId, request, ct));

    // Domain rule violations (DomainException) become 400s with a clear
    // message; the client learns exactly which business rule it broke.
    private async Task<ActionResult<OrderDto>> Handle(Func<Task<OrderDto>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
