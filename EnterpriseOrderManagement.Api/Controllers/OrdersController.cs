using EnterpriseOrderManagement.Api.Dtos.Orders;
using EnterpriseOrderManagement.Api.Dtos.Common;
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
//GET BY ID
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

//GET
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<OrderResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<OrderResponse>>> GetOrders(
        [FromQuery] OrderQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        if (parameters.FromDate.HasValue
            && parameters.ToDate.HasValue
            && parameters.FromDate > parameters.ToDate)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid date range.",
                Detail = "FromDate must be earlier than or equal to ToDate."
            });
        }
        var result = await orderService.GetPagedAsync(
            parameters,
            cancellationToken);
        return Ok(result);
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

//CONFIRM Order
    [HttpPost("{id:int}/confirm")]
    [ProducesResponseType(
        typeof(OrderResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> ConfirmOrder(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await orderService.ConfirmAsync(
            id,
            cancellationToken);
        return ToStatusChangeResponse(result);
    }

//CANCEL Order
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(
        typeof(OrderResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> CancelOrder(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await orderService.CancelAsync(
            id,
            cancellationToken);
        return ToStatusChangeResponse(result);
    }

    private ActionResult<OrderResponse> ToStatusChangeResponse(
        OrderStatusChangeResult result)
    {
        if (result.Status == OrderStatusChangeStatus.Success)
        {
            return Ok(result.Order);
        }
        if (result.Status == OrderStatusChangeStatus.OrderNotFound)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Order not found.",
                Detail = "The requested order does not exist."
            });
        }
        if (result.Status == OrderStatusChangeStatus.ProductNotFound)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Product not found.",
                Detail = $"Product with ID {result.RelatedId} was not found."
            });
        }
        if (result.Status == OrderStatusChangeStatus.InvalidTransition)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Invalid order status transition.",
                Detail = "The requested status change is not allowed for the current order status."
            });
        }
        return Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "An unexpected error occurred.");
    }
}