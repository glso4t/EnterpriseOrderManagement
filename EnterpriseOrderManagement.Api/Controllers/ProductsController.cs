using EnterpriseOrderManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseOrderManagement.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetProducts()
    {
        var products = new List<Product>
        {
            new Product
            {
                Id = 1,
                Name = "Laptop",
                Price = 899.99m,
                Stock = 10
            },

            new Product
            {
                Id = 2,
                Name = "Mouse",
                Price = 25.50m,
                Stock = 50
            }
        };

        return Ok(products);
    }
}