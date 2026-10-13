using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using EnterpriseOrderManagement.Api.Dtos.Products;

namespace EnterpriseOrderManagement.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(
    IProductService productService)
    : ControllerBase
{
//GET
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<Product>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetProducts(
        CancellationToken cancellationToken)
    {
        var products = await productService.GetAllAsync(cancellationToken);
        return Ok(products);
    }

//GET BY ID
    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(Product),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> GetProductById(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(
            id,
            cancellationToken);
        if (product is null)
        {
            return NotFound();
        }
        return Ok(product);
    }

//POST
    [HttpPost]
    [ProducesResponseType(
        typeof(Product),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Product>> CreateProduct(
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
    [HttpPut("{id:int}")]
    [ProducesResponseType(
        typeof(Product),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> UpdateProduct(
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
            return NotFound();
        }
        return Ok(updatedProduct);
    }

//DELETE
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
            return NotFound();
        }
        return NoContent();
    }
}