using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseOrderManagement.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<Product>> GetProducts()
    {
        var products = _productService.GetAll();

        return Ok(products);
    }
}