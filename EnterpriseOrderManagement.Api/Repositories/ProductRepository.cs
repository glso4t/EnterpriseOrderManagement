using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Repositories.Interfaces;

namespace EnterpriseOrderManagement.Api.Repositories;

public class ProductRepository : IProductRepository
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

    public IReadOnlyList<Product> GetAll()
    {
        return [.. _products];
    }
}