using EnterpriseOrderManagement.Api.Dtos.Orders;
using EnterpriseOrderManagement.Api.Services.Interfaces;
using EnterpriseOrderManagement.Api.Services.Results;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseOrderManagement.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(
    IOrderService orderService) : ControllerBase
{
//GET BY ID ONLY
    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(OrderResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetOrderById(
        int id,
        CancellationToken cancellationToken)
    {
        var order = await orderService.GetByIdAsync(
            id,
            cancellationToken);
        if (order is null)
        {
            return NotFound();
        }
        return Ok(order);
    }

//POST
    [HttpPost]
    [ProducesResponseType(
        typeof(OrderResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await orderService.CreateAsync(
            request,
            cancellationToken);
        if (result.Status == OrderCreationStatus.CustomerNotFound)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Customer not found.",
                Detail = $"Customer with ID {result.RelatedId} was not found."
            });
        }
        if (result.Status == OrderCreationStatus.ProductNotFound)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Product not found.",
                Detail = $"Product with ID {result.RelatedId} was not found."
            });
        }
        if (result.Status == OrderCreationStatus.InsufficientStock)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Insufficient stock.",
                Detail = $"Product with ID {result.RelatedId} does not have enough stock."
            });
        }
        if (result.Status == OrderCreationStatus.DuplicateProductItems)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Duplicate product items.",
                Detail = "Each product must appear only once in the order request."
            });
        }
        var createdOrder = result.Order!;
        return CreatedAtAction(
            nameof(GetOrderById),
            new { id = createdOrder.Id },
            createdOrder);
    }
}