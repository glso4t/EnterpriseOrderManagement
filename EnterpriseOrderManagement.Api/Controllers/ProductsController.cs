using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using EnterpriseOrderManagement.Api.Dtos.Products;
using Microsoft.AspNetCore.Authorization;

namespace EnterpriseOrderManagement.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/products")]
public class ProductsController(
    IProductService productService)
    : ControllerBase
{
    private ObjectResult ProductNotFound(int id)
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Product not found.",
            detail: $"Product with ID {id} was not found.");
    }

//GET
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<ProductResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetProducts(
        CancellationToken cancellationToken)
    {
        var products = await productService.GetAllAsync(cancellationToken);
        return Ok(products);
    }

//GET BY ID
    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(ProductResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetProductById(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(
            id,
            cancellationToken);
        if (product is null)
        {
            return ProductNotFound(id);
        }
        return Ok(product);
    }

//POST
    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    [ProducesResponseType(
        typeof(ProductResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductResponse>> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var createdProduct = await productService.CreateAsync(
            request,
            cancellationToken);
        return CreatedAtAction(
            nameof(GetProductById),
            new { id = createdProduct.Id },
            createdProduct);
    }

//PUT
    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(
        typeof(ProductResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(
        int id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var updatedProduct = await productService.UpdateAsync(
            id,
            request,
            cancellationToken);
        if (updatedProduct is null)
        {
            return ProductNotFound(id);
        }
        return Ok(updatedProduct);
    }

//PATCH
    [Authorize(Policy = "AdminOnly")]
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> PatchProduct(
        int id,
        [FromBody] PatchProductRequest request,
        CancellationToken cancellationToken)
    {
        var updatedProduct = await productService.PatchAsync(
            id,
            request,
            cancellationToken);
        if (updatedProduct is null)
        {
            return ProductNotFound(id);
        }
        return Ok(updatedProduct);
    }

//DELETE
    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await productService.DeleteAsync(
            id,
            cancellationToken);
        if (!deleted)
        {
            return ProductNotFound(id);
        }
        return NoContent();
    }
}