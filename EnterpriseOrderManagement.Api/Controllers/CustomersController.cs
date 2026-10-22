using EnterpriseOrderManagement.Api.Dtos.Customers;
using EnterpriseOrderManagement.Api.Services.Interfaces;
using EnterpriseOrderManagement.Api.Services.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EnterpriseOrderManagement.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/customers")]
public sealed class CustomersController(
    ICustomerService customerService) : ControllerBase
{
    private ConflictObjectResult EmailConflict()
    {
        return Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Email already exists.",
            Detail = "A customer with this email address already exists."
        });
    }

//GET
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<CustomerResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> GetCustomers(
        CancellationToken cancellationToken)
    {
        var customers = await customerService.GetAllAsync(
            cancellationToken);
        return Ok(customers);
    }

//GET BY ID
    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(CustomerResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> GetCustomerById(
        int id,
        CancellationToken cancellationToken)
    {
        var customer = await customerService.GetByIdAsync(
            id,
            cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }
        return Ok(customer);
    }

//POST
    [HttpPost]
    [ProducesResponseType(
        typeof(CustomerResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await customerService.CreateAsync(
            request,
            cancellationToken);
        if (result.Status == CustomerWriteStatus.EmailAlreadyExists)
        {
            return EmailConflict();
        }
        var createdCustomer = result.Customer!;
        return CreatedAtAction(
            nameof(GetCustomerById),
            new { id = createdCustomer.Id },
            createdCustomer);
    }

//PUT
    [HttpPut("{id:int}")]
    [ProducesResponseType(
        typeof(CustomerResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerResponse>> UpdateCustomer(
        int id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await customerService.UpdateAsync(
            id,
            request,
            cancellationToken);
        if (result.Status == CustomerWriteStatus.NotFound)
        {
            return NotFound();
        }
        if (result.Status == CustomerWriteStatus.EmailAlreadyExists)
        {
            return EmailConflict();
        }
        return Ok(result.Customer);
    }

//DELETE
    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCustomer(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await customerService.DeleteAsync(
            id,
            cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }
}