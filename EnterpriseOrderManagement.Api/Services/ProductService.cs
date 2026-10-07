using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Services.Interfaces;

namespace EnterpriseOrderManagement.Api.Services;

public class ProductService : IProductService
{
    private readonly List<Product> _products =
    [
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
    ];

    public IEnumerable<Product> GetAll()
    {
        return _products;
    }
}