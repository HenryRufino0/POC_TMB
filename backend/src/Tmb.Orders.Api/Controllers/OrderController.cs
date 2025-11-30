using Microsoft.AspNetCore.Mvc;
using Tmb.Orders.Application.Orders;
using Tmb.Orders.Api.Messaging;   

namespace Tmb.Orders.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly OrderCreatedPublisher _orderCreatedPublisher;

    public OrdersController(
        IOrderService orderService,
        OrderCreatedPublisher orderCreatedPublisher)  
    {
        _orderService = orderService;
        _orderCreatedPublisher = orderCreatedPublisher;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetAllAsync(cancellationToken);
        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);

        if (order is null)
            return NotFound();

        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        
        var created = await _orderService.CreateAsync(request, cancellationToken);

       
        await _orderCreatedPublisher.PublishAsync(created.Id, cancellationToken);

        
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
